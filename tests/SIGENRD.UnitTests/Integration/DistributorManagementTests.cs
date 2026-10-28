using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Gestión de EDE: el administrador las crea, edita y desactiva; el ingeniero y la distribuidora pertenecen a una EDE
/// y no ven ni tocan lo de otra (solicitudes, transformadores, reportes).
/// </summary>
[Collection(ApiCollection.Name)]
public class DistributorManagementTests(ApiFactory api)
{
    private const string TemporaryPassword = "Temporal-Ede-2026";

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, (int)response.StatusCode + ": " + text);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static object Ede(string name, decimal available = 70, decimal conditioned = 30) => new
    {
        name, type = "EDENORTE", contactEmail = "contacto@edenorte.test", contactPhone = "809-555-0101",
        availableThresholdPct = available, conditionedThresholdPct = conditioned, defaultPowerFactor = 0.9m
    };

    private async Task<int> NewEdeAsync()
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var created = await Data(await admin.PostAsJsonAsync("/api/v1/Distributors/manage", Ede("Edenorte " + Unique())));
        return created.GetProperty("id").GetInt32();
    }

    /// <summary>Personal nuevo de una EDE, con la contraseña temporal ya cambiada: devuelve su id, correo y token.</summary>
    private async Task<(string Id, string Email, string Token)> NewStaffAsync(string role, int distributorId)
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var email = "ede-" + Unique() + "@test.energymap.local";
        var created = await admin.PostAsJsonAsync("/api/v1/Auth/register-internal",
            new { fullName = "Personal " + email[..12], email, password = TemporaryPassword, userType = role, distributorId });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var userId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("userId").GetString()!;

        var mail = api.Emails.Sent.Single(e => e.To == email);
        var href = Regex.Match(mail.Body, "href='([^']+)'").Groups[1].Value;
        (await api.CreateClient().GetAsync(new Uri(href).PathAndQuery)).EnsureSuccessStatusCode();

        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await TokenAsync(email, TemporaryPassword));
        var changed = await client.PostAsJsonAsync("/api/v1/Auth/change-password",
            new { currentPassword = TemporaryPassword, newPassword = ApiFactory.TestPassword });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var token = JsonDocument.Parse(await changed.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;

        return (userId, email, token);
    }

    private async Task<string> TokenAsync(string email, string password)
    {
        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;
    }

    private HttpClient With(string token)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private sealed record Seeded(int RequestId, int TransformerId, string TransformerCode);

    /// <summary>Un transformador y una solicitud recibida en la EDE indicada.</summary>
    private async Task<Seeded> SeedRequestAsync(int distributorId, decimal total = 1000, decimal available = 800)
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);
        await using var context = api.NewAppContext();

        var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona EDE " + Unique() }).Entity;
        await context.SaveChangesAsync();

        var transformer = new Transformer
        {
            Code = "TR-EDE-" + Unique(),
            DistributorId = distributorId,
            ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(19.4, -70.7)
        };
        transformer.UpdateCapacity(total, available, 70, 30);
        context.Transformers.Add(transformer);

        var customer = await TestData.CustomerForAsync(context, customerUser.UserId);
        await context.SaveChangesAsync();

        var request = context.ConnectionRequests.Add(new ConnectionRequest
        {
            RequestCode = "REQ-EDE-" + Unique(),
            CustomerId = customer.Id,
            DistributorId = distributorId,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10
        }).Entity;
        await context.SaveChangesAsync();

        return new Seeded(request.Id, transformer.Id, transformer.Code);
    }

    private static async Task<int> ListedCount(HttpClient client, Seeded seeded)
    {
        var data = await Data(await client.GetAsync("/api/v1/ConnectionRequest?transformerId=" + seeded.TransformerId));
        return data.GetProperty("items").GetArrayLength();
    }

    // ---------------- catálogo de EDE ----------------

    [FactIfDatabase]
    public async Task El_administrador_crea_edita_desactiva_y_reactiva_una_EDE()
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var name = "Edenorte " + Unique();

        var created = await Data(await admin.PostAsJsonAsync("/api/v1/Distributors/manage", Ede(name)));
        var id = created.GetProperty("id").GetInt32();
        Assert.Equal("EDENORTE", created.GetProperty("type").GetString());
        Assert.True(created.GetProperty("isActive").GetBoolean());

        // El nombre no se repite, aunque cambien las mayúsculas.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/v1/Distributors/manage", Ede(name.ToUpperInvariant()))).StatusCode);
        // El umbral de condicionada tiene que quedar por debajo del de disponible.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/v1/Distributors/manage", Ede("Otra " + Unique(), available: 30, conditioned: 50))).StatusCode);

        var updated = await Data(await admin.PutAsJsonAsync("/api/v1/Distributors/manage/" + id, Ede(name, available: 60, conditioned: 25)));
        Assert.Equal(60m, updated.GetProperty("availableThresholdPct").GetDecimal());

        var publicList = api.CreateClient();
        Assert.Contains(id, (await Data(await publicList.GetAsync("/api/v1/Distributors"))).EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));

        await Data(await admin.PostAsync("/api/v1/Distributors/manage/" + id + "/deactivate", null));
        Assert.DoesNotContain(id, (await Data(await publicList.GetAsync("/api/v1/Distributors"))).EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));
        var managed = (await Data(await admin.GetAsync("/api/v1/Distributors/manage"))).EnumerateArray().Single(d => d.GetProperty("id").GetInt32() == id);
        Assert.False(managed.GetProperty("isActive").GetBoolean());

        await Data(await admin.PostAsync("/api/v1/Distributors/manage/" + id + "/reactivate", null));
        Assert.True((await Data(await admin.GetAsync("/api/v1/Distributors/manage/" + id))).GetProperty("isActive").GetBoolean());
    }

    [FactIfDatabase]
    public async Task Solo_el_administrador_gestiona_las_EDE()
    {
        foreach (var role in new[] { TestRole.Distributor, TestRole.Engineer, TestRole.Customer, TestRole.InstallerCompany })
        {
            var client = await api.ClientFor(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/Distributors/manage")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/Distributors/manage", Ede("X " + Unique()))).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/v1/Distributors/manage")).StatusCode);
    }

    [FactIfDatabase]
    public async Task Cambiar_los_umbrales_reclasifica_los_transformadores_de_la_EDE()
    {
        var edeId = await NewEdeAsync();
        var seeded = await SeedRequestAsync(edeId, total: 1000, available: 500); // 50 %: condicionada con 70/30

        var admin = await api.ClientFor(TestRole.Admin);
        await Data(await admin.PutAsJsonAsync("/api/v1/Distributors/manage/" + edeId, Ede("Edenorte " + Unique(), available: 40, conditioned: 20)));

        await using var context = api.NewAppContext();
        var transformer = await context.Transformers.AsNoTracking().SingleAsync(t => t.Id == seeded.TransformerId);
        Assert.Equal(TransformerStatus.Available, transformer.Status);
    }

    // ---------------- personal de una EDE ----------------

    [FactIfDatabase]
    public async Task El_alta_interna_del_personal_exige_una_EDE_activa()
    {
        var admin = await api.ClientFor(TestRole.Admin);
        object Body(string userType, int? distributorId) => new
        {
            fullName = "Interno", email = "alta-" + Unique() + "@test.energymap.local", password = TemporaryPassword, userType, distributorId
        };

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/Auth/register-internal", Body("Engineer", null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/Auth/register-internal", Body("Distributor", 999_999))).StatusCode);

        var edeId = await NewEdeAsync();
        await Data(await admin.PostAsync("/api/v1/Distributors/manage/" + edeId + "/deactivate", null));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/Auth/register-internal", Body("Distributor", edeId))).StatusCode);

        // El administrador no pertenece a ninguna EDE: lo que indique se descarta.
        var adminBody = Body("Admin", await api.EdesurIdAsync());
        var created = await admin.PostAsJsonAsync("/api/v1/Auth/register-internal", adminBody);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var userId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("userId").GetString()!;
        var listed = (await Data(await admin.GetAsync("/api/v1/Users?role=Admin&pageSize=100"))).GetProperty("items").EnumerateArray()
            .Single(u => u.GetProperty("id").GetString() == userId);
        Assert.Equal(JsonValueKind.Null, listed.GetProperty("distributorId").ValueKind);
    }

    [FactIfDatabase]
    public async Task El_personal_solo_ve_y_gestiona_lo_de_su_EDE()
    {
        var edenorte = await NewEdeAsync();
        var inEdesur = await SeedRequestAsync(await api.EdesurIdAsync());
        var inEdenorte = await SeedRequestAsync(edenorte);
        var (_, _, token) = await NewStaffAsync("Distributor", edenorte);
        var staff = With(token);

        // Solicitudes: la de Edesur no existe para el personal de Edenorte.
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync("/api/v1/ConnectionRequest/" + inEdesur.RequestId)).StatusCode);
        Assert.Equal(0, await ListedCount(staff, inEdesur));
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);
        Assert.Equal(1, await ListedCount(staff, inEdenorte));
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PostAsync("/api/v1/ConnectionRequest/" + inEdesur.RequestId + "/start-review", null)).StatusCode);

        // Y al revés: la distribuidora de Edesur no ve la de Edenorte; el administrador ve las dos.
        var edesurStaff = await api.ClientFor(TestRole.Distributor);
        Assert.Equal(HttpStatusCode.NotFound, (await edesurStaff.GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);
        var admin = await api.ClientFor(TestRole.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/ConnectionRequest/" + inEdesur.RequestId)).StatusCode);

        // Red: el transformador de Edesur no sale en su lista ni se puede tocar.
        var managed = await Data(await staff.GetAsync("/api/v1/Transformers/manage?code=" + inEdesur.TransformerCode));
        Assert.Equal(0, managed.GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PutAsJsonAsync("/api/v1/Transformers/" + inEdesur.TransformerId + "/capacity",
            new { totalCapacityKva = 1000, availableCapacityKva = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PutAsJsonAsync("/api/v1/Transformers/" + inEdesur.TransformerId + "/maintenance",
            new { inMaintenance = true })).StatusCode);
        var own = await Data(await staff.GetAsync("/api/v1/Transformers/manage?code=" + inEdenorte.TransformerCode));
        Assert.Equal(1, own.GetProperty("totalCount").GetInt32());

        // El mapa público sigue mostrando todo: el transformador de Edesur se ve sin sesión.
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync("/api/v1/Transformers/" + inEdesur.TransformerId)).StatusCode);
    }

    [FactIfDatabase]
    public async Task El_personal_crea_transformadores_en_su_EDE_aunque_pida_otra()
    {
        var edenorte = await NewEdeAsync();
        var (_, _, token) = await NewStaffAsync("Distributor", edenorte);
        int zoneId;
        await using (var context = api.NewAppContext())
        {
            var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona alta " + Unique() }).Entity;
            await context.SaveChangesAsync();
            zoneId = zone.Id;
        }

        var code = "TR-ALTA-" + Unique();
        var created = await Data(await With(token).PostAsJsonAsync("/api/v1/Transformers", new
        {
            code, distributorId = await api.EdesurIdAsync(), serviceZoneId = zoneId,
            totalCapacityKva = 100, availableCapacityKva = 90, latitude = 19.4, longitude = -70.7
        }));

        await using var check = api.NewAppContext();
        Assert.Equal(edenorte, (await check.Transformers.AsNoTracking().SingleAsync(t => t.Id == created.GetInt32())).DistributorId);
    }

    [FactIfDatabase]
    public async Task Asignar_otra_EDE_invalida_el_token_y_cambia_lo_que_ve()
    {
        var edenorte = await NewEdeAsync();
        var inEdenorte = await SeedRequestAsync(edenorte);
        var (userId, email, token) = await NewStaffAsync("Engineer", await api.EdesurIdAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await With(token).GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);

        var admin = await api.ClientFor(TestRole.Admin);
        var assigned = await Data(await admin.PutAsJsonAsync("/api/v1/Users/" + userId + "/distributor", new { distributorId = edenorte }));
        Assert.Equal(edenorte, assigned.GetProperty("distributorId").GetInt32());

        // El token viejo llevaba la EDE anterior: deja de servir.
        Assert.Equal(HttpStatusCode.Unauthorized, (await With(token).GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);
        var fresh = With(await TokenAsync(email, ApiFactory.TestPassword));
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/v1/ConnectionRequest/" + inEdenorte.RequestId)).StatusCode);

        // El administrador no se asigna a una EDE, y la EDE tiene que existir.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/Users/" + userId + "/distributor", new { distributorId = 999_999 })).StatusCode);
    }
}
