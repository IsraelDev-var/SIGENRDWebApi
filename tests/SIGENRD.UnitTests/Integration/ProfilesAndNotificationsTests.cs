using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Perfiles de cliente e instaladora, validación de empresas (RN-05, RF-10), notificaciones internas (RF-32),
/// lista con filtros y seguimiento público por expediente (RF-14).
/// </summary>
[Collection(ApiCollection.Name)]
public class ProfilesAndNotificationsTests(ApiFactory api)
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Read(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        return doc.RootElement.Clone();
    }

    private static string CustomerBody(string? nationalId = null) =>
        "{\"fullName\":\"Ana Pérez\",\"nationalIdOrRnc\":\"" + (nationalId ?? Unique() + "001") +
        "\",\"email\":\"ana@test.energymap.local\",\"phone\":\"809-555-0100\"}";

    private async Task<int> UnreadFor(TestRole role)
    {
        var response = await (await api.ClientFor(role)).GetAsync("/api/v1/Notifications/unread-count");
        return (await Read(response)).GetProperty("data").GetInt32();
    }

    // ---------------- perfil de cliente ----------------

    [FactIfDatabase]
    public async Task El_cliente_crea_y_consulta_su_perfil_una_sola_vez()
    {
        var (client, _, _) = await api.NewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/Customers/me")).StatusCode);

        var created = await client.PostAsync("/api/v1/Customers/me", Json(CustomerBody()));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var again = await client.PostAsync("/api/v1/Customers/me", Json(CustomerBody()));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        var mine = await Read(await client.GetAsync("/api/v1/Customers/me"));
        Assert.Equal("Ana Pérez", mine.GetProperty("data").GetProperty("fullName").GetString());
    }

    [FactIfDatabase]
    public async Task Dos_clientes_no_pueden_tener_la_misma_cedula()
    {
        var (first, _, _) = await api.NewUserAsync();
        var (second, _, _) = await api.NewUserAsync();
        var nationalId = Unique() + "777";

        Assert.Equal(HttpStatusCode.OK, (await first.PostAsync("/api/v1/Customers/me", Json(CustomerBody(nationalId)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await second.PostAsync("/api/v1/Customers/me", Json(CustomerBody(nationalId)))).StatusCode);
    }

    [FactIfDatabase]
    public async Task El_perfil_de_cliente_no_lo_crea_otro_rol()
    {
        var response = await (await api.ClientFor(TestRole.Distributor)).PostAsync("/api/v1/Customers/me", Json(CustomerBody()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------- empresas instaladoras ----------------

    [FactIfDatabase]
    public async Task La_empresa_se_registra_pendiente_y_la_distribuidora_la_valida()
    {
        var (client, userId, _) = await api.NewUserAsync(installer: true);
        var unreadBefore = await UnreadFor(TestRole.Distributor);

        // Registro: queda Pendiente y la distribuidora recibe un aviso.
        var registered = await client.PostAsync("/api/v1/InstallerCompanies/me",
            Json("{\"tradeName\":\"Solar Norte\",\"rnc\":\"" + Unique() + "\",\"contactEmail\":\"info@solarnorte.test\"}"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var company = (await Read(registered)).GetProperty("data");
        Assert.Equal("Pending", company.GetProperty("validationStatus").GetString());
        var id = company.GetProperty("id").GetInt32();

        Assert.Equal(unreadBefore + 1, await UnreadFor(TestRole.Distributor));

        // Aparece entre las pendientes.
        var pending = await Read(await (await api.ClientFor(TestRole.Distributor)).GetAsync("/api/v1/InstallerCompanies?status=1"));
        Assert.Contains(pending.GetProperty("data").EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);

        // Una empresa pendiente no puede validarse a sí misma ni la valida un cliente.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/InstallerCompanies/" + id + "/validate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await (await api.ClientFor(TestRole.Customer)).PostAsync("/api/v1/InstallerCompanies/" + id + "/validate", null)).StatusCode);

        // La valida la distribuidora y la empresa recibe su aviso.
        var validated = await (await api.ClientFor(TestRole.Distributor)).PostAsync("/api/v1/InstallerCompanies/" + id + "/validate", null);
        Assert.Equal(HttpStatusCode.OK, validated.StatusCode);
        Assert.Equal("Validated", (await Read(validated)).GetProperty("data").GetProperty("validationStatus").GetString());

        var mine = await Read(await client.GetAsync("/api/v1/Notifications?unreadOnly=true"));
        Assert.Contains(mine.GetProperty("data").GetProperty("items").EnumerateArray(),
            n => n.GetProperty("type").GetString() == "InstallerValidated");

        // Validar de nuevo es un error.
        var again = await (await api.ClientFor(TestRole.Admin)).PostAsync("/api/v1/InstallerCompanies/" + id + "/validate", null);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        Assert.False(string.IsNullOrEmpty(userId));
    }

    [FactIfDatabase]
    public async Task Dos_empresas_no_pueden_tener_el_mismo_RNC()
    {
        var (first, _, _) = await api.NewUserAsync(installer: true);
        var (second, _, _) = await api.NewUserAsync(installer: true);
        var rnc = Unique();
        var body = "{\"tradeName\":\"Empresa\",\"rnc\":\"" + rnc + "\"}";

        Assert.Equal(HttpStatusCode.OK, (await first.PostAsync("/api/v1/InstallerCompanies/me", Json(body))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await second.PostAsync("/api/v1/InstallerCompanies/me", Json(body))).StatusCode);
    }

    // ---------------- notificaciones ----------------

    /// <summary>Crea el borrador y, si <paramref name="submit"/>, sube los documentos requeridos y lo envía (D-4).</summary>
    private async Task<(int RequestId, string Code)> CreateRequestAsCustomerAsync(bool submit = true)
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);

        await using var context = api.NewAppContext();
        var distributor = await context.Distributors.FirstOrDefaultAsync();
        if (distributor is null)
        {
            distributor = new Distributor { Name = "Edesur" };
            context.Distributors.Add(distributor);
        }

        var zone = new ServiceZone { ZoneName = "Zona " + Unique() };
        context.ServiceZones.Add(zone);
        await context.SaveChangesAsync();

        var transformer = new Transformer
        {
            Code = "TR-NT-" + Unique(), DistributorId = distributor.Id, ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.4, -69.9)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);
        context.Transformers.Add(transformer);

        var customer = await TestData.CustomerForAsync(context, customerUser.UserId);

        var body = "{\"customerId\":" + customer.Id + ",\"distributorId\":" + distributor.Id +
                   ",\"transformerId\":" + transformer.Id + ",\"requestType\":1,\"requestedCapacityKva\":5," +
                   "\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1,\"projectAddress\":\"Calle " + Unique() + "\"," +
                   "\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";

        var response = await (await api.ClientFor(TestRole.Customer)).PostAsync("/api/v1/ConnectionRequest", Json(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var id = (await Read(response)).GetProperty("data").GetInt32();
        var request = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == id);
        if (!submit) return (id, request.RequestCode);

        foreach (var requirement in RequiredDocuments.For(request, false))
        {
            context.TechnicalDocuments.Add(new TechnicalDocument
            {
                ConnectionRequestId = id, DocumentType = requirement.AnyOf[0], FileName = "d.pdf",
                FileUrl = "https://blob.test/" + Unique(), ContentType = "application/pdf", SizeBytes = 1
            });
        }
        await context.SaveChangesAsync();

        var sent = await (await api.ClientFor(TestRole.Customer)).PostAsync("/api/v1/ConnectionRequest/" + id + "/submit", null);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        return (id, request.RequestCode);
    }

    [FactIfDatabase]
    public async Task Crear_una_solicitud_avisa_al_personal_y_confirma_al_solicitante()
    {
        var staffBefore = await UnreadFor(TestRole.Distributor);
        var customerBefore = await UnreadFor(TestRole.Customer);

        var (_, code) = await CreateRequestAsCustomerAsync();

        Assert.Equal(staffBefore + 1, await UnreadFor(TestRole.Distributor));
        Assert.Equal(customerBefore + 1, await UnreadFor(TestRole.Customer));

        var staff = await Read(await (await api.ClientFor(TestRole.Distributor)).GetAsync("/api/v1/Notifications?unreadOnly=true"));
        Assert.Contains(staff.GetProperty("data").GetProperty("items").EnumerateArray(),
            n => n.GetProperty("title").GetString()!.Contains(code));
    }

    [FactIfDatabase]
    public async Task Un_borrador_no_avisa_a_nadie_ni_se_ve_fuera_del_solicitante()
    {
        var staffBefore = await UnreadFor(TestRole.Distributor);
        var customerBefore = await UnreadFor(TestRole.Customer);

        var (id, code) = await CreateRequestAsCustomerAsync(submit: false);

        Assert.Equal(staffBefore, await UnreadFor(TestRole.Distributor));
        Assert.Equal(customerBefore, await UnreadFor(TestRole.Customer));

        // El personal no lo ve ni por id ni en la lista; el seguimiento público tampoco.
        var staff = await api.ClientFor(TestRole.Engineer);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync("/api/v1/ConnectionRequest/" + id)).StatusCode);
        var list = await Read(await staff.GetAsync("/api/v1/ConnectionRequest/search?text=" + code));
        Assert.Equal(0, list.GetProperty("data").GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound,
            (await (await api.ClientFor(TestRole.Anonymous)).GetAsync("/api/v1/ConnectionRequest/track/" + code)).StatusCode);

        // El solicitante sí, como borrador.
        var own = await Read(await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest/" + id));
        Assert.Equal("Draft", own.GetProperty("data").GetProperty("status").GetString());
    }

    [FactIfDatabase]
    public async Task Enviar_un_borrador_sin_documentos_falla_y_con_documentos_pasa_a_recibida()
    {
        var (id, code) = await CreateRequestAsCustomerAsync(submit: false);
        var customer = await api.ClientFor(TestRole.Customer);

        var missing = await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/submit", null);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        await using (var context = api.NewAppContext())
        {
            var request = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == id);
            foreach (var requirement in RequiredDocuments.For(request, false))
                context.TechnicalDocuments.Add(new TechnicalDocument
                {
                    ConnectionRequestId = id, DocumentType = requirement.AnyOf[0], FileName = "d.pdf",
                    FileUrl = "https://blob.test/" + Unique(), ContentType = "application/pdf", SizeBytes = 1
                });
            await context.SaveChangesAsync();
        }

        var sent = await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/submit", null);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);

        var again = await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/submit", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var tracked = await Read(await (await api.ClientFor(TestRole.Anonymous)).GetAsync("/api/v1/ConnectionRequest/track/" + code));
        Assert.Equal("Received", tracked.GetProperty("data").GetProperty("status").GetString());

        // Otro usuario no puede enviar borradores ajenos.
        var (otherId, _) = await CreateRequestAsCustomerAsync(submit: false);
        var (stranger, _, _) = await api.NewUserAsync(installer: false);
        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.PostAsync("/api/v1/ConnectionRequest/" + otherId + "/submit", null)).StatusCode);
    }

    [FactIfDatabase]
    public async Task Descartar_un_borrador_lo_cancela_sin_avisar()
    {
        var (id, _) = await CreateRequestAsCustomerAsync(submit: false);
        var customer = await api.ClientFor(TestRole.Customer);
        var before = await UnreadFor(TestRole.Customer);

        var cancel = await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/cancel", Json("{}"));
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(before, await UnreadFor(TestRole.Customer));

        var own = await Read(await customer.GetAsync("/api/v1/ConnectionRequest/" + id));
        Assert.Equal("Cancelled", own.GetProperty("data").GetProperty("status").GetString());
    }

    [FactIfDatabase]
    public async Task Marcar_notificaciones_como_leidas_y_no_tocar_las_ajenas()
    {
        await CreateRequestAsCustomerAsync();

        var staff = await api.ClientFor(TestRole.Distributor);
        var list = await Read(await staff.GetAsync("/api/v1/Notifications?unreadOnly=true&pageSize=1"));
        var first = list.GetProperty("data").GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();

        // Otro usuario no puede marcarla: responde como si no existiera.
        var stranger = await (await api.ClientFor(TestRole.Customer)).PostAsync("/api/v1/Notifications/" + first + "/read", null);
        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);

        var before = await UnreadFor(TestRole.Distributor);
        var read = await staff.PostAsync("/api/v1/Notifications/" + first + "/read", null);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(before - 1, await UnreadFor(TestRole.Distributor));

        var all = await staff.PostAsync("/api/v1/Notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.Equal(0, await UnreadFor(TestRole.Distributor));
    }

    [FactIfDatabase]
    public async Task Las_notificaciones_requieren_sesion_y_tienen_limite_de_pagina()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await (await api.ClientFor(TestRole.Anonymous)).GetAsync("/api/v1/Notifications")).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/Notifications?pageSize=1000")).StatusCode);
    }

    // ---------------- lista con filtros y paginación ----------------

    [FactIfDatabase]
    public async Task La_lista_filtra_pagina_y_respeta_el_alcance()
    {
        var (_, codeA) = await CreateRequestAsCustomerAsync();
        await CreateRequestAsCustomerAsync();
        await CreateRequestAsCustomerAsync();

        var customer = await api.ClientFor(TestRole.Customer);

        var page = await Read(await customer.GetAsync("/api/v1/ConnectionRequest/search?pageSize=2&page=1"));
        var data = page.GetProperty("data");
        Assert.Equal(2, data.GetProperty("items").GetArrayLength());
        Assert.True(data.GetProperty("totalCount").GetInt32() >= 3);
        Assert.True(data.GetProperty("totalPages").GetInt32() >= 2);

        var byCode = await Read(await customer.GetAsync("/api/v1/ConnectionRequest/search?text=" + codeA.ToLowerInvariant()));
        var found = byCode.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(codeA, Assert.Single(found).GetProperty("requestCode").GetString());

        var byStatus = await Read(await customer.GetAsync("/api/v1/ConnectionRequest/search?status=6&pageSize=100")); // Rejected
        Assert.All(byStatus.GetProperty("data").GetProperty("items").EnumerateArray(),
            r => Assert.Equal("Rejected", r.GetProperty("status").GetString()));

        // Una instaladora sin vínculo no ve ninguna.
        var installer = await Read(await (await api.ClientFor(TestRole.InstallerCompany))
            .GetAsync("/api/v1/ConnectionRequest/search?text=" + codeA));
        Assert.Equal(0, installer.GetProperty("data").GetProperty("totalCount").GetInt32());

        // El personal sí.
        var staff = await Read(await (await api.ClientFor(TestRole.Engineer)).GetAsync("/api/v1/ConnectionRequest/search?text=" + codeA));
        Assert.Equal(1, staff.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [TheoryIfDatabase]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("status=99")]
    [InlineData("from=2026-12-31&to=2026-01-01")]
    public async Task La_lista_rechaza_parametros_invalidos(string query)
    {
        var response = await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/ConnectionRequest/search?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task El_texto_de_busqueda_no_actua_como_comodin()
    {
        await CreateRequestAsCustomerAsync();

        var response = await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/ConnectionRequest/search?text=%25");
        var data = (await Read(response)).GetProperty("data");

        Assert.Equal(0, data.GetProperty("totalCount").GetInt32());
    }

    // ---------------- seguimiento público ----------------

    [FactIfDatabase]
    public async Task El_seguimiento_publico_solo_muestra_el_estado()
    {
        var (_, code) = await CreateRequestAsCustomerAsync();
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        var response = await anonymous.GetAsync("/api/v1/ConnectionRequest/track/" + code.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = (await Read(response)).GetProperty("data");
        Assert.Equal(code, data.GetProperty("requestCode").GetString());
        Assert.Equal("Received", data.GetProperty("status").GetString());

        // Nada que identifique a las personas o al inmueble.
        var names = data.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "lastUpdatedAt", "requestCode", "requestType", "requestedAt", "status" }.OrderBy(n => n, StringComparer.Ordinal).ToArray(), names);
    }

    [FactIfDatabase]
    public async Task Un_expediente_inexistente_es_404()
    {
        var response = await (await api.ClientFor(TestRole.Anonymous)).GetAsync("/api/v1/ConnectionRequest/track/REQ-NOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- reenvío avisa al personal ----------------

    [FactIfDatabase]
    public async Task Reenviar_una_solicitud_corregida_avisa_al_personal()
    {
        var (id, code) = await CreateRequestAsCustomerAsync();

        var staff = await api.ClientFor(TestRole.Distributor);
        var customer = await api.ClientFor(TestRole.Customer);
        await staff.PostAsync("/api/v1/ConnectionRequest/" + id + "/start-review", null);
        await staff.PostAsync("/api/v1/ConnectionRequest/" + id + "/observe", Json("{\"comment\":\"Falta información\"}"));
        await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/start-correction", null);

        var before = await UnreadFor(TestRole.Distributor);
        var resubmit = await customer.PostAsync("/api/v1/ConnectionRequest/" + id + "/resubmit", null);
        Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);

        Assert.Equal(before + 1, await UnreadFor(TestRole.Distributor));

        var list = await Read(await staff.GetAsync("/api/v1/Notifications?unreadOnly=true"));
        Assert.Contains(list.GetProperty("data").GetProperty("items").EnumerateArray(),
            n => n.GetProperty("title").GetString()!.Contains(code) && n.GetProperty("title").GetString()!.Contains("reenviada"));
    }
}
