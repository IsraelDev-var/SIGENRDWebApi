using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// El formulario de solicitud (Anexo A del reglamento de interconexión) se guarda como datos y la API informa
/// qué documentos faltan según el trámite.
/// </summary>
[Collection(ApiCollection.Name)]
public class RequestFormTests(ApiFactory api)
{
    private sealed record Parties(int CustomerId, int DistributorId, int TransformerId);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private async Task<Parties> SeedPartiesAsync(bool legalEntity = false)
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
            Code = "TR-FORM-" + Unique(), DistributorId = distributor.Id, ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.4, -69.9)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);

        var customer = new Customer
        {
            ApplicationUserId = customerUser.UserId, FullName = "Dueño", NationalIdOrRnc = Unique() + "000",
            IsLegalEntity = legalEntity
        };
        context.AddRange(transformer, customer);
        await context.SaveChangesAsync();

        return new Parties(customer.Id, distributor.Id, transformer.Id);
    }

    private static string NetMeteringBody(Parties p, string extra = "") =>
        "{\"customerId\":" + p.CustomerId + ",\"distributorId\":" + p.DistributorId + ",\"transformerId\":" + p.TransformerId +
        ",\"requestType\":2,\"requestedCapacityKva\":8,\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1," +
        "\"projectAddress\":\"Calle 1 #10\",\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true," +
        "\"engineerFullName\":\"Ing. Ana Perez\",\"engineerCodiaNumber\":\"12345\",\"engineerCompany\":\"Solar RD\"," +
        "\"serviceContractNumber\":\"C-1001\",\"meterNumber\":\"M-2002\"," +
        "\"netMetering\":{\"generationSystemType\":1,\"installedCapacityKw\":7.5,\"generators\":[" +
        "{\"generationSystemType\":1,\"generatorKind\":3,\"manufacturer\":\"Huawei\",\"inverterModel\":\"SUN2000-8KTL\"," +
        "\"serialNumber\":\"SN-777\",\"phases\":1,\"ratedPowerKva\":8,\"connectionVoltage\":240,\"nominalCurrent\":33," +
        "\"powerFactorMin\":0.9,\"powerFactorMax\":1,\"harmonicDistortionPercent\":2.5," +
        "\"certificationStandard\":\"IEEE 1547\",\"certifyingUnit\":\"UL\"}]}" + extra + "}";

    private async Task<(HttpStatusCode Status, JsonElement Body)> Post(TestRole role, string json)
    {
        var response = await (await api.ClientFor(role)).PostAsync("/api/v1/ConnectionRequest", Json(json));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (response.StatusCode, doc.RootElement.Clone());
    }

    [FactIfDatabase]
    public async Task La_medicion_neta_guarda_el_formulario_y_los_generadores()
    {
        var parties = await SeedPartiesAsync();

        var created = await Post(TestRole.Customer, NetMeteringBody(parties));
        Assert.Equal(HttpStatusCode.OK, created.Status);
        var id = created.Body.GetProperty("data").GetInt32();

        await using var context = api.NewAppContext();
        var request = await context.ConnectionRequests.AsNoTracking()
            .Include(r => r.NetMeteringRequest!).ThenInclude(n => n.Generators)
            .SingleAsync(r => r.Id == id);

        Assert.Equal(RequestType.NetMetering, request.RequestType);
        Assert.Equal("12345", request.EngineerCodiaNumber);
        Assert.Equal("C-1001", request.ServiceContractNumber);
        Assert.Equal("M-2002", request.MeterNumber);
        Assert.Equal(PropertyRelation.Owner, request.PropertyRelation);
        Assert.NotNull(request.DeclarationAcceptedAt);

        var details = Assert.IsType<NetMeteringRequest>(request.NetMeteringRequest);
        Assert.Equal(7.5m, details.InstalledCapacityKw);

        var generator = Assert.Single(details.Generators!);
        Assert.Equal("Huawei", generator.Manufacturer);
        Assert.Equal(GeneratorKind.InverterBased, generator.GeneratorKind);
        Assert.Equal("IEEE 1547", generator.CertificationStandard);
        Assert.Equal(0.9m, generator.PowerFactorMin);
    }

    [FactIfDatabase]
    public async Task La_medicion_neta_sin_datos_del_sistema_se_rechaza()
    {
        var parties = await SeedPartiesAsync();
        var body = NetMeteringBody(parties).Replace("\"netMetering\":{", "\"netMeteringX\":{");

        var result = await Post(TestRole.Customer, body);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
    }

    [FactIfDatabase]
    public async Task Un_inquilino_debe_indicar_al_dueno()
    {
        var parties = await SeedPartiesAsync();
        var body = NetMeteringBody(parties).Replace("\"declarationAccepted\":true", "\"declarationAccepted\":true,\"propertyRelation\":3");

        var result = await Post(TestRole.Customer, body);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        var errors = result.Body.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains(errors, e => e!.Contains("dueño"));
    }

    [FactIfDatabase]
    public async Task Los_documentos_requeridos_dependen_del_tramite_y_del_dueno()
    {
        var parties = await SeedPartiesAsync();
        var body = NetMeteringBody(parties).Replace(
            "\"declarationAccepted\":true",
            "\"declarationAccepted\":true,\"propertyRelation\":3,\"ownerFullName\":\"Luis Dueño\",\"ownerNationalId\":\"00100000009\"");

        var created = await Post(TestRole.Customer, body);
        Assert.Equal(HttpStatusCode.OK, created.Status);
        var id = created.Body.GetProperty("data").GetInt32();

        var response = await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest/" + id + "/required-documents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("complete").GetBoolean());
        var codes = data.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToArray();
        Assert.Equal(new[] { "IDENTIDAD", "PLANO_SITUACION", "UNIFILAR", "EQUIPOS", "DUENO" }, codes);
        Assert.All(data.GetProperty("items").EnumerateArray(), i => Assert.False(i.GetProperty("satisfied").GetBoolean()));
    }

    [FactIfDatabase]
    public async Task Al_subir_documentos_los_requisitos_se_van_cumpliendo()
    {
        var parties = await SeedPartiesAsync();
        var created = await Post(TestRole.Customer, NetMeteringBody(parties));
        var id = created.Body.GetProperty("data").GetInt32();

        await using (var context = api.NewAppContext())
        {
            foreach (var type in new[] { DocumentType.IdentityDocument, DocumentType.Blueprint })
                context.TechnicalDocuments.Add(new TechnicalDocument
                {
                    ConnectionRequestId = id, DocumentType = type, FileName = type + ".pdf",
                    FileUrl = "https://blob.test/" + Unique(), ContentType = "application/pdf", SizeBytes = 10
                });
            await context.SaveChangesAsync();
        }

        var response = await (await api.ClientFor(TestRole.Distributor)).GetAsync("/api/v1/ConnectionRequest/" + id + "/required-documents");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var satisfied = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("satisfied").GetBoolean()).Select(i => i.GetProperty("code").GetString()).ToArray();

        Assert.Equal(new[] { "IDENTIDAD", "UNIFILAR" }, satisfied);
    }

    [FactIfDatabase]
    public async Task Los_documentos_requeridos_no_se_ven_en_una_solicitud_ajena()
    {
        var parties = await SeedPartiesAsync();
        var created = await Post(TestRole.Customer, NetMeteringBody(parties));
        var id = created.Body.GetProperty("data").GetInt32();

        var response = await (await api.ClientFor(TestRole.InstallerCompany)).GetAsync("/api/v1/ConnectionRequest/" + id + "/required-documents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task Una_persona_juridica_necesita_los_documentos_de_la_empresa()
    {
        var parties = await SeedPartiesAsync(legalEntity: true);
        var body = "{\"customerId\":" + parties.CustomerId + ",\"distributorId\":" + parties.DistributorId +
                   ",\"transformerId\":" + parties.TransformerId + ",\"requestType\":1,\"requestedCapacityKva\":5," +
                   "\"usageType\":3,\"tariffType\":1,\"interconnectionType\":1,\"projectAddress\":\"Zona industrial\"," +
                   "\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";

        var created = await Post(TestRole.Customer, body);
        Assert.Equal(HttpStatusCode.OK, created.Status);
        var id = created.Body.GetProperty("data").GetInt32();

        var response = await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest/" + id + "/required-documents");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var codes = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString()).ToArray();

        Assert.Contains("RNC", codes);
        Assert.Contains("REGISTRO_MERCANTIL", codes);
        Assert.Contains("REGISTRO_INDUSTRIAL", codes); // tarifa industrial
    }
}
