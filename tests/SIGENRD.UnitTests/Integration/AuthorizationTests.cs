using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Autorización de punta a punta: la API real, la base real y un token real por rol.
/// Para cada endpoint se comprueba el código que debe recibir cada rol:
///   - anónimo sin permiso: 401
///   - autenticado sin permiso: 403
///   - con permiso: el código del propio endpoint (400 o 404 cuando los datos de prueba no existen),
///     que prueba que la petición pasó la autorización y llegó al handler.
/// </summary>
public class AuthorizationTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly TestRole[] AllRoles =
        { TestRole.Anonymous, TestRole.Customer, TestRole.InstallerCompany, TestRole.Engineer, TestRole.Distributor, TestRole.Admin };

    private static readonly TestRole[] Anyone = AllRoles;
    private static readonly TestRole[] Staff = { TestRole.Distributor, TestRole.Admin };
    private static readonly TestRole[] CanCreateRequests = { TestRole.Customer, TestRole.InstallerCompany, TestRole.Admin };
    private static readonly TestRole[] AuthenticatedAll =
        { TestRole.Customer, TestRole.InstallerCompany, TestRole.Engineer, TestRole.Distributor, TestRole.Admin };

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed record Endpoint(string Name, HttpMethod Method, string Url, Func<HttpContent?> Body, TestRole[] Allowed, HttpStatusCode WhenAllowed);

    private static readonly Endpoint[] Endpoints =
    {
        new("mapa", HttpMethod.Get, "/api/v1/Transformers?MinLatitude=18.4&MaxLatitude=18.6&MinLongitude=-70&MaxLongitude=-69.8", () => null, Anyone, HttpStatusCode.OK),
        new("busqueda", HttpMethod.Get, "/api/v1/Transformers/search?Latitude=18.48&Longitude=-69.93", () => null, Anyone, HttpStatusCode.OK),
        new("detalle", HttpMethod.Get, "/api/v1/Transformers/999999", () => null, Anyone, HttpStatusCode.NotFound),
        new("zonas criticas", HttpMethod.Get, "/api/v1/Transformers/critical-zones", () => null, Staff, HttpStatusCode.OK),
        new("crear transformador", HttpMethod.Post, "/api/v1/Transformers",
            () => Json("{\"code\":\"TR-AUTH\",\"distributorId\":999999,\"serviceZoneId\":1,\"totalCapacityKva\":100,\"availableCapacityKva\":50,\"latitude\":18.5,\"longitude\":-69.9}"),
            Staff, HttpStatusCode.BadRequest),
        new("actualizar capacidad", HttpMethod.Put, "/api/v1/Transformers/999999/capacity",
            () => Json("{\"totalCapacityKva\":100,\"availableCapacityKva\":50}"), Staff, HttpStatusCode.NotFound),
        new("mantenimiento", HttpMethod.Put, "/api/v1/Transformers/999999/maintenance",
            () => Json("{\"inMaintenance\":true}"), Staff, HttpStatusCode.NotFound),
        new("importar CSV", HttpMethod.Post, "/api/v1/Transformers/import", () =>
        {
            var form = new MultipartFormDataContent { { new StringContent("999999"), "distributorId" } };
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes("code,serviceZoneId,totalCapacityKva,availableCapacityKva,latitude,longitude\n"));
            file.Headers.ContentType = new("text/csv");
            form.Add(file, "file", "transformadores.csv");
            return form;
        }, Staff, HttpStatusCode.BadRequest),
        new("crear solicitud", HttpMethod.Post, "/api/v1/ConnectionRequest", () => Json("{}"), CanCreateRequests, HttpStatusCode.BadRequest),
        new("listar solicitudes", HttpMethod.Get, "/api/v1/ConnectionRequest", () => null, AuthenticatedAll, HttpStatusCode.OK),
        new("alta de usuario interno", HttpMethod.Post, "/api/v1/Auth/register-internal", () => Json("{\"fullName\":\"X\",\"email\":\"\",\"password\":\"\",\"userType\":2}"),
            new[] { TestRole.Admin }, HttpStatusCode.OK),
    };

    public static IEnumerable<object[]> Matrix()
    {
        foreach (var endpoint in Endpoints)
            foreach (var role in AllRoles)
                yield return new object[] { endpoint.Name, role };
    }

    [TheoryIfDatabase]
    [MemberData(nameof(Matrix))]
    public async Task Cada_rol_recibe_el_codigo_que_le_corresponde(string endpointName, TestRole role)
    {
        var endpoint = Endpoints.Single(e => e.Name == endpointName);
        var client = await api.ClientFor(role);

        var request = new HttpRequestMessage(endpoint.Method, endpoint.Url) { Content = endpoint.Body() };

        // El alta interna necesita un correo único y válido por ejecución.
        if (endpoint.Name == "alta de usuario interno")
        {
            var email = "interno-" + role + "-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";
            request.Content = Json("{\"fullName\":\"Interno\",\"email\":\"" + email + "\",\"password\":\"" + ApiFactory.TestPassword + "\",\"userType\":2}");
        }

        var response = await client.SendAsync(request);

        HttpStatusCode expected = endpoint.Allowed.Contains(role)
            ? endpoint.WhenAllowed
            : role == TestRole.Anonymous ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;

        Assert.True(expected == response.StatusCode,
            endpointName + " como " + role + ": se esperaba " + (int)expected + " y llegó " + (int)response.StatusCode);
    }

    // ---------------- registro y credenciales ----------------

    private static StringContent Register(string userType) => Json(
        "{\"fullName\":\"Alta publica\",\"email\":\"publico-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local\"," +
        "\"password\":\"" + ApiFactory.TestPassword + "\",\"userType\":" + userType + "}");

    [TheoryIfDatabase]
    [InlineData(2)] // Engineer
    [InlineData(4)] // Distributor
    [InlineData(6)] // Admin
    public async Task El_registro_publico_no_permite_crear_roles_internos(int userType)
    {
        var response = await (await api.ClientFor(TestRole.Anonymous)).PostAsync("/api/v1/Auth/register", Register(userType.ToString()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TheoryIfDatabase]
    [InlineData(1, "Customer")]
    [InlineData(3, "InstallerCompany")]
    public async Task El_registro_publico_si_permite_clientes_e_instaladoras(int userType, string role)
    {
        var response = await (await api.ClientFor(TestRole.Anonymous)).PostAsync("/api/v1/Auth/register", Register(userType.ToString()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(role, doc.RootElement.GetProperty("role").GetString());
    }

    [FactIfDatabase]
    public async Task No_existen_credenciales_por_defecto_conocidas()
    {
        var response = await (await api.ClientFor(TestRole.Anonymous)).PostAsJsonAsync("/api/v1/Auth/login",
            new { email = "admin@sigenrd.com", password = "Pa$$word123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task Un_token_invalido_es_401()
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "esto.no.es.un.jwt");

        var response = await client.GetAsync("/api/v1/Transformers/critical-zones");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task El_token_de_un_rol_no_sirve_para_otro()
    {
        // Un cliente no puede usar su token para endpoints de la distribuidora, aunque cambie de ruta.
        var customer = await api.ClientFor(TestRole.Customer);

        var response = await customer.PostAsync("/api/v1/Auth/register-internal",
            Json("{\"fullName\":\"X\",\"email\":\"escalada@test.energymap.local\",\"password\":\"" + ApiFactory.TestPassword + "\",\"userType\":6}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------- acceso a datos ajenos (RN-22) ----------------

    private async Task<int> CreateRequestOwnedBy(TestRole owner)
    {
        var ownerSession = await api.LoginAsync(owner);

        await using var context = api.NewAppContext();
        var distributor = await context.Distributors.FirstOrDefaultAsync();
        if (distributor is null)
        {
            distributor = new Distributor { Name = "Edesur" };
            context.Distributors.Add(distributor);
        }

        var zone = new ServiceZone { ZoneName = "Zona IDOR " + Guid.NewGuid().ToString("N")[..6] };
        context.ServiceZones.Add(zone);
        await context.SaveChangesAsync();

        var transformer = new Transformer
        {
            Code = "TR-IDOR-" + Guid.NewGuid().ToString("N")[..6],
            DistributorId = distributor.Id,
            ServiceZoneId = zone.Id,
            Location = SIGENRD.Infrastructure.Persistences.Repositories.TransformerQueries.ToPoint(18.4, -69.9)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);

        var customer = new Customer { ApplicationUserId = ownerSession.UserId, FullName = "Dueño", NationalIdOrRnc = Guid.NewGuid().ToString("N")[..11] };
        context.AddRange(transformer, customer);
        await context.SaveChangesAsync();

        var request = new ConnectionRequest
        {
            RequestCode = "REQ-IDOR-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = customer.Id,
            DistributorId = distributor.Id,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10
        };
        context.ConnectionRequests.Add(request);
        await context.SaveChangesAsync();

        return request.Id;
    }

    // Hallazgo abierto (RN-22): GET /ConnectionRequest/{id} devuelve cualquier solicitud a cualquier rol autenticado.
    // Se corrige en la fase 3, junto con el flujo de solicitudes. Quitar el Skip cuando se corrija.
    [FactIfDatabase(Skip = "Hallazgo abierto SEC-03 (RN-22): sin filtro por dueño. Se corrige en la fase 3.")]
    public async Task Una_instaladora_no_puede_leer_la_solicitud_de_un_cliente()
    {
        var requestId = await CreateRequestOwnedBy(TestRole.Customer);

        var response = await (await api.ClientFor(TestRole.InstallerCompany)).GetAsync("/api/v1/ConnectionRequest/" + requestId);

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            "devolvió " + (int)response.StatusCode);
    }

    [FactIfDatabase(Skip = "Hallazgo abierto SEC-03 (RN-22): sin filtro por dueño. Se corrige en la fase 3.")]
    public async Task El_listado_de_un_cliente_solo_incluye_sus_solicitudes()
    {
        var requestId = await CreateRequestOwnedBy(TestRole.InstallerCompany);

        var response = await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"id\":" + requestId + ",", body);
    }
}
