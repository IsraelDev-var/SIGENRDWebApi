using System.Net;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>Reportes por la API real (RF-27, RF-28, RF-30), con una distribuidora propia para que los números sean exactos.</summary>
[Collection(ApiCollection.Name)]
public class ReportsApiTests(ApiFactory api)
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private sealed record Seeded(int DistributorId, int InstallerA, int InstallerB);

    /// <summary>
    /// Cuatro solicitudes de marzo de 2026 en una distribuidora nueva:
    /// aprobada (revisión a 1 día, resolución a 3), rechazada (2 y 6), recibida y cancelada.
    /// </summary>
    private async Task<Seeded> SeedAsync()
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);
        await using var context = api.NewAppContext();

        var distributor = context.Distributors.Add(new Distributor { Name = "Dist reportes " + Unique() }).Entity;
        var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona rep " + Unique() }).Entity;
        await context.SaveChangesAsync();

        var transformer = new Transformer
        {
            Code = "TR-REP-" + Unique(),
            DistributorId = distributor.Id,
            ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.6, -70.2)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);
        context.Transformers.Add(transformer);

        var a = context.InstallerCompanies.Add(new InstallerCompany { TradeName = "Solar A " + Unique(), Rnc = "1" + Unique() }).Entity;
        var b = context.InstallerCompanies.Add(new InstallerCompany { TradeName = "Solar B " + Unique(), Rnc = "2" + Unique() }).Entity;
        var customer = await TestData.CustomerForAsync(context, customerUser.UserId);
        await context.SaveChangesAsync();

        var march = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        ConnectionRequest Request(RequestStatus status, int day, int? installer) => context.ConnectionRequests.Add(new ConnectionRequest
        {
            RequestCode = "REQ-REP-" + Unique(),
            CustomerId = customer.Id,
            InstallerCompanyId = installer,
            DistributorId = distributor.Id,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10,
            RequestedAt = march.AddDays(day),
            Status = status
        }).Entity;

        var approved = Request(RequestStatus.Approved, 0, a.Id);
        var rejected = Request(RequestStatus.Rejected, 1, a.Id);
        Request(RequestStatus.Received, 2, b.Id);
        Request(RequestStatus.Cancelled, 3, null);
        await context.SaveChangesAsync();

        // El historial es de solo inserción y el contexto fija CreatedAt al guardar: se inserta con su fecha real.
        async Task History(ConnectionRequest r, string state, double days) =>
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "StateHistories" ("ConnectionRequestId", "PreviousState", "NewState", "CreatedAt", "IsActive") VALUES ({r.Id}, 'x', {state}, {r.RequestedAt.AddDays(days)}, true)""");

        await History(approved, "UnderReview", 1);
        await History(approved, "Approved", 3);
        await History(rejected, "UnderReview", 2);
        await History(rejected, "Rejected", 6);

        return new Seeded(distributor.Id, a.Id, b.Id);
    }

    private static string Period(Seeded s) => "from=2026-03-01&to=2026-04-01&distributorId=" + s.DistributorId;

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, response.StatusCode + ": " + text);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("data").Clone();
    }

    [FactIfDatabase]
    public async Task Las_estadisticas_resumen_el_periodo()
    {
        var seeded = await SeedAsync();
        // Las solicitudes son de una EDE nueva: solo el administrador elige la EDE del reporte.
        var client = await api.ClientFor(TestRole.Admin);

        var s = await Data(await client.GetAsync("/api/v1/Reports/requests/statistics?" + Period(seeded)));

        Assert.Equal(4, s.GetProperty("total").GetInt32());
        Assert.Equal(1, s.GetProperty("pending").GetInt32());
        Assert.Equal(1, s.GetProperty("approved").GetInt32());
        Assert.Equal(1, s.GetProperty("rejected").GetInt32());
        Assert.Equal(1, s.GetProperty("cancelled").GetInt32());
        Assert.Equal(50m, s.GetProperty("approvalRatePct").GetDecimal());
        Assert.Equal(1.5m, s.GetProperty("avgDaysToFirstReview").GetDecimal());
        Assert.Equal(4.5m, s.GetProperty("avgDaysToResolution").GetDecimal());

        var month = Assert.Single(s.GetProperty("byMonth").EnumerateArray());
        Assert.Equal("2026-03", month.GetProperty("month").GetString());
        Assert.Equal(4, month.GetProperty("total").GetInt32());

        // Fuera del período no cuenta.
        var april = await Data(await client.GetAsync("/api/v1/Reports/requests/statistics?from=2026-04-01&to=2026-05-01&distributorId=" + seeded.DistributorId));
        Assert.Equal(0, april.GetProperty("total").GetInt32());
    }

    [FactIfDatabase]
    public async Task El_ranking_pone_primero_a_la_instaladora_mas_activa()
    {
        var seeded = await SeedAsync();

        var ranking = await Data(await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/Reports/installers/ranking?" + Period(seeded)));
        var items = ranking.EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(seeded.InstallerA, items[0].GetProperty("installerCompanyId").GetInt32());
        Assert.Equal(2, items[0].GetProperty("total").GetInt32());
        Assert.Equal(1, items[0].GetProperty("rank").GetInt32());
        Assert.Equal(seeded.InstallerB, items[1].GetProperty("installerCompanyId").GetInt32());
    }

    [FactIfDatabase]
    public async Task Exporta_a_Excel_y_PDF()
    {
        var seeded = await SeedAsync();
        var client = await api.ClientFor(TestRole.Admin);

        var xlsx = await client.GetAsync("/api/v1/Reports/requests/export?format=Xlsx&" + Period(seeded));
        Assert.Equal(HttpStatusCode.OK, xlsx.StatusCode);
        Assert.Equal("reporte-solicitudes-20260301-20260331.xlsx", xlsx.Content.Headers.ContentDisposition!.FileName?.Trim('"'));
        using (var workbook = new XLWorkbook(new MemoryStream(await xlsx.Content.ReadAsByteArrayAsync())))
            Assert.Equal(5, workbook.Worksheet("Detalle").LastRowUsed()!.RowNumber()); // encabezado + 4

        var pdf = await client.GetAsync("/api/v1/Reports/requests/export?format=Pdf&" + Period(seeded));
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
    }

    [FactIfDatabase]
    public async Task La_distribuidora_solo_ve_los_reportes_de_su_EDE_aunque_pida_otra()
    {
        var seeded = await SeedAsync();
        var client = await api.ClientFor(TestRole.Distributor);

        var s = await Data(await client.GetAsync("/api/v1/Reports/requests/statistics?" + Period(seeded)));
        Assert.Equal(0, s.GetProperty("total").GetInt32());

        var xlsx = await client.GetAsync("/api/v1/Reports/requests/export?format=Xlsx&" + Period(seeded));
        using var workbook = new XLWorkbook(new MemoryStream(await xlsx.Content.ReadAsByteArrayAsync()));
        Assert.Equal(1, workbook.Worksheet("Detalle").LastRowUsed()!.RowNumber()); // solo el encabezado
    }

    [FactIfDatabase]
    public async Task Valida_el_periodo_y_solo_lo_ve_la_distribuidora_o_el_administrador()
    {
        var distributor = await api.ClientFor(TestRole.Distributor);
        Assert.Equal(HttpStatusCode.BadRequest, (await distributor.GetAsync("/api/v1/Reports/requests/statistics?from=2026-05-01&to=2026-04-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await distributor.GetAsync("/api/v1/Reports/requests/statistics?from=2020-01-01&to=2026-01-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await distributor.GetAsync("/api/v1/Reports/installers/ranking?top=0")).StatusCode);

        // Por defecto: últimos 12 meses.
        Assert.Equal(HttpStatusCode.OK, (await distributor.GetAsync("/api/v1/Reports/requests/statistics")).StatusCode);

        foreach (var role in new[] { TestRole.Customer, TestRole.InstallerCompany, TestRole.Engineer })
            Assert.Equal(HttpStatusCode.Forbidden, (await (await api.ClientFor(role)).GetAsync("/api/v1/Reports/requests/statistics")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/v1/Reports/requests/statistics")).StatusCode);
    }
}
