using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// RN-22: cada solicitante ve y crea solo lo suyo; el personal de la distribuidora ve todo.
/// Estas pruebas cierran el hallazgo SEC-03.
/// </summary>
[Collection(ApiCollection.Name)]
public class RequestAccessTests(ApiFactory api)
{
    private sealed record Seeded(int RequestId, int CustomerId, int InstallerCompanyId, int TransformerId, int DistributorId);

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Crea una solicitud cuyo cliente es el usuario Customer y, si se pide, cuya instaladora es el usuario InstallerCompany.
    /// </summary>
    private async Task<Seeded> SeedAsync(bool withInstaller, bool installerValidated = true)
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);
        var installerUser = await api.LoginAsync(TestRole.InstallerCompany);

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
            Code = "TR-ACC-" + Unique(),
            DistributorId = distributor.Id,
            ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.4, -69.9)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);

        var customer = await TestData.CustomerForAsync(context, customerUser.UserId);
        var company = await TestData.CompanyForAsync(context, installerUser.UserId, installerValidated);
        context.Transformers.Add(transformer);
        await context.SaveChangesAsync();

        var request = new ConnectionRequest
        {
            RequestCode = "REQ-ACC-" + Unique(),
            CustomerId = customer.Id,
            InstallerCompanyId = withInstaller ? company.Id : null,
            DistributorId = distributor.Id,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10
        };
        context.ConnectionRequests.Add(request);
        await context.SaveChangesAsync();

        return new Seeded(request.Id, customer.Id, company.Id, transformer.Id, distributor.Id);
    }

    private static async Task<bool> ListContains(HttpClient client, Seeded seeded)
    {
        // El listado es paginado: se filtra por el transformador propio de la prueba para que quepa en una página.
        var response = await client.GetAsync("/api/v1/ConnectionRequest?transformerId=" + seeded.TransformerId);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
            .Any(r => r.GetProperty("id").GetInt32() == seeded.RequestId);
    }

    private static string CreateBody(Seeded s, int customerId, int? installerCompanyId) =>
        "{\"customerId\":" + customerId +
        (installerCompanyId.HasValue ? ",\"installerCompanyId\":" + installerCompanyId : "") +
        ",\"distributorId\":" + s.DistributorId + ",\"transformerId\":" + s.TransformerId +
        ",\"requestType\":1,\"requestedCapacityKva\":10,\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1," +
        "\"projectAddress\":\"Calle 1\",\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";

    // ---------------- lectura ----------------

    [FactIfDatabase]
    public async Task El_cliente_dueño_ve_su_solicitud_en_el_listado_y_por_id()
    {
        var seeded = await SeedAsync(withInstaller: false);
        var customer = await api.ClientFor(TestRole.Customer);

        Assert.True(await ListContains(customer, seeded));
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId)).StatusCode);
    }

    [FactIfDatabase]
    public async Task Una_instaladora_ajena_no_ve_la_solicitud_ni_en_el_listado_ni_por_id()
    {
        var seeded = await SeedAsync(withInstaller: false);
        var installer = await api.ClientFor(TestRole.InstallerCompany);

        Assert.False(await ListContains(installer, seeded));
        Assert.Equal(HttpStatusCode.NotFound, (await installer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId)).StatusCode);
    }

    [FactIfDatabase]
    public async Task La_instaladora_de_la_solicitud_si_la_ve_y_el_cliente_tambien()
    {
        var seeded = await SeedAsync(withInstaller: true);

        var installer = await api.ClientFor(TestRole.InstallerCompany);
        var customer = await api.ClientFor(TestRole.Customer);

        Assert.True(await ListContains(installer, seeded));
        Assert.True(await ListContains(customer, seeded));
        Assert.Equal(HttpStatusCode.OK, (await installer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId)).StatusCode);
    }

    [TheoryIfDatabase]
    [InlineData(TestRole.Engineer)]
    [InlineData(TestRole.Distributor)]
    [InlineData(TestRole.Admin)]
    public async Task El_personal_de_la_distribuidora_ve_todas_las_solicitudes(TestRole role)
    {
        var seeded = await SeedAsync(withInstaller: true);
        var client = await api.ClientFor(role);

        Assert.True(await ListContains(client, seeded));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId)).StatusCode);
    }

    [FactIfDatabase]
    public async Task Una_solicitud_inexistente_es_404_con_formato_camelCase()
    {
        var response = await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/ConnectionRequest/99999999");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("\"succeeded\":false", body);
    }

    [FactIfDatabase]
    public async Task Los_errores_de_validacion_usan_el_mismo_formato_que_las_respuestas_correctas()
    {
        var response = await (await api.ClientFor(TestRole.Anonymous)).GetAsync("/api/v1/Transformers");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"succeeded\":false", body);
        Assert.Contains("\"errors\":[", body);
    }

    // ---------------- creación ----------------

    [FactIfDatabase]
    public async Task El_cliente_crea_una_solicitud_a_su_nombre()
    {
        var seeded = await SeedAsync(withInstaller: false);
        var customer = await api.ClientFor(TestRole.Customer);

        var response = await customer.PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, null)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("data").GetInt32() > 0);
    }

    [FactIfDatabase]
    public async Task El_cliente_no_puede_crear_a_nombre_de_otro()
    {
        var seeded = await SeedAsync(withInstaller: false);

        // Un cliente cuyo dueño es otro usuario (la instaladora de prueba).
        int otherCustomerId;
        await using (var context = api.NewAppContext())
        {
            var other = new Customer { ApplicationUserId = "otro-usuario-" + Unique(), FullName = "Otro", NationalIdOrRnc = Unique() + "111" };
            context.Customers.Add(other);
            await context.SaveChangesAsync();
            otherCustomerId = other.Id;
        }

        var response = await (await api.ClientFor(TestRole.Customer))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, otherCustomerId, null)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task El_cliente_no_puede_asignar_una_instaladora()
    {
        var seeded = await SeedAsync(withInstaller: true);

        var response = await (await api.ClientFor(TestRole.Customer))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, seeded.InstallerCompanyId)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task La_instaladora_validada_crea_con_su_propia_empresa()
    {
        var seeded = await SeedAsync(withInstaller: true);

        var response = await (await api.ClientFor(TestRole.InstallerCompany))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, seeded.InstallerCompanyId)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task La_instaladora_sin_validar_no_puede_crear_solicitudes()
    {
        var seeded = await SeedAsync(withInstaller: true, installerValidated: false);

        var response = await (await api.ClientFor(TestRole.InstallerCompany))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, seeded.InstallerCompanyId)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task La_instaladora_no_puede_usar_la_empresa_de_otra()
    {
        var seeded = await SeedAsync(withInstaller: true);

        int otherCompanyId;
        await using (var context = api.NewAppContext())
        {
            var other = new InstallerCompany
            {
                TradeName = "Ajena " + Unique(),
                Rnc = Unique(),
                ApplicationUserId = "otro-usuario-" + Unique(),
                ValidationStatus = InstallerValidationStatus.Validated
            };
            context.InstallerCompanies.Add(other);
            await context.SaveChangesAsync();
            otherCompanyId = other.Id;
        }

        var response = await (await api.ClientFor(TestRole.InstallerCompany))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, otherCompanyId)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TheoryIfDatabase]
    [InlineData(TestRole.Admin)]
    [InlineData(TestRole.Distributor)]
    [InlineData(TestRole.Engineer)]
    public async Task El_personal_no_crea_solicitudes_en_nombre_de_otros(TestRole role)
    {
        // Matriz de permisos 3.2: crear solicitudes es solo del cliente y de la instaladora validada.
        var seeded = await SeedAsync(withInstaller: false);

        var response = await (await api.ClientFor(role))
            .PostAsync("/api/v1/ConnectionRequest", Json(CreateBody(seeded, seeded.CustomerId, null)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
