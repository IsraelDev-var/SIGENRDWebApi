using System.Text;
using System.Text.Json;
using SIGENRD.Core.Application.Features.Simulations;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Application;

/// <summary>CA-10: el resumen en PDF de la simulación.</summary>
public class SimulationReportTests
{
    [Fact]
    public void Genera_el_reporte_en_PDF()
    {
        var simulation = new SimulationDto
        {
            Id = 42,
            Name = "Casa de María",
            CreatedAt = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc),
            MonthlyConsumptionKwh = 400,
            InstalledCapacityKw = 2,
            InvestmentCost = 120000,
            Latitude = 18.4861,
            Longitude = -69.9312,
            AnnualProductionKwh = 3000,
            AnnualConsumptionKwh = 4800,
            CoveragePct = 62.5m,
            AnnualBillBefore = 40296,
            AnnualBillAfter = 10890,
            AnnualSavings = 29406,
            PaybackYears = 4.08m,
            Roi = 477.27m,
            ViabilityStatus = "Profitable",
            ReferenceTransformer = new NearbyTransformerDto { Code = "TR-0001", DistanceMeters = 120, Status = "Available" },
            Recommendations = new List<string>
            {
                "El sistema cubre el 62 % de tu consumo y es rentable: podrías ampliarlo a unos 2.9 kWp para cubrir cerca del 90 %."
            },
            FormulaVersion = "v1",
            Assumptions = JsonDocument.Parse("{}").RootElement
        };

        var pdf = new SimulationReportGenerator().Generate(simulation);

        // Para revisarlo a ojo: ENERGYMAP_REPORT_OUT=/ruta/simulacion.pdf
        var output = Environment.GetEnvironmentVariable("ENERGYMAP_REPORT_OUT");
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllBytes(output, pdf);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
