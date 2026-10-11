using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using SIGENRD.Core.Application.Features.Reports;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Shared.Services;
using SIGENRDWebApi.Configuration;

namespace SIGENRD.UnitTests.Application;

/// <summary>Reportes (RF-27, RF-28, RF-30) sin base de datos: cálculos y exportación.</summary>
public class ReportsTests
{
    private static readonly DateTime March = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    private static RequestReportRow Row(int id, RequestStatus status, DateTime requestedAt, int? installer = null,
        double? reviewAfterDays = null, double? resolvedAfterDays = null, string zone = "Naco", decimal kva = 10, string code = "")
        => new(id, code == "" ? "REQ-" + id : code, RequestType.Connection, status, requestedAt, kva, "Edesur", zone,
            installer, installer is null ? null : "Instaladora " + installer,
            reviewAfterDays is null ? null : requestedAt.AddDays(reviewAfterDays.Value),
            resolvedAfterDays is null ? null : requestedAt.AddDays(resolvedAfterDays.Value));

    private static readonly IReadOnlyList<RequestReportRow> Rows = new[]
    {
        Row(1, RequestStatus.Approved, March, installer: 7, reviewAfterDays: 1, resolvedAfterDays: 3, kva: 20),
        Row(2, RequestStatus.Rejected, March.AddDays(2), installer: 7, reviewAfterDays: 2, resolvedAfterDays: 6, zone: "Gazcue"),
        Row(3, RequestStatus.Received, March.AddDays(25), installer: 8),
        Row(4, RequestStatus.Cancelled, March.AddMonths(1)),
        Row(5, RequestStatus.Approved, March.AddMonths(1), installer: 8, reviewAfterDays: 1, resolvedAfterDays: 12, kva: 5),
        Row(6, RequestStatus.InCorrection, March.AddMonths(1), reviewAfterDays: 4)
    };

    [Fact]
    public void Calcula_totales_tasas_y_tiempos()
    {
        var s = ReportCalculator.Statistics(Rows, March.AddDays(-9), March.AddMonths(2));

        Assert.Equal(6, s.Total);
        Assert.Equal(2, s.Pending);          // Recibida y En corrección
        Assert.Equal(2, s.Approved);
        Assert.Equal(1, s.Rejected);
        Assert.Equal(1, s.Cancelled);
        Assert.Equal(66.7m, s.ApprovalRatePct); // 2 de 3 resueltas
        Assert.Equal(65m, s.RequestedKva);      // 20 + 10 + 10 + 10 + 5 + 10
        Assert.Equal(25m, s.ApprovedKva);
        Assert.Equal(2m, s.AvgDaysToFirstReview);   // (1 + 2 + 1 + 4) / 4
        Assert.Equal(7m, s.AvgDaysToResolution);    // (3 + 6 + 12) / 3
        Assert.Equal(6m, s.MedianDaysToResolution);
    }

    [Fact]
    public void Desglosa_por_mes_estado_y_zona()
    {
        var s = ReportCalculator.Statistics(Rows, March.AddDays(-9), March.AddMonths(2));

        Assert.Equal(new[] { new MonthItem("2026-03", 2, 1, 1), new MonthItem("2026-04", 4, 1, 0) }, s.ByMonth);
        Assert.Equal(new CountItem("Approved", 2), s.ByStatus[0]);
        Assert.Equal(new CountItem("Naco", 5), s.ByZone[0]);
        Assert.Equal(new CountItem("Gazcue", 1), s.ByZone[1]);
    }

    [Fact]
    public void Sin_solicitudes_no_hay_tasas_ni_tiempos()
    {
        var s = ReportCalculator.Statistics(Array.Empty<RequestReportRow>(), March, March.AddDays(1));

        Assert.Equal(0, s.Total);
        Assert.Null(s.ApprovalRatePct);
        Assert.Null(s.AvgDaysToResolution);
        Assert.Null(s.MedianDaysToResolution);
    }

    [Fact]
    public void El_ranking_ordena_por_solicitudes_y_luego_por_aprobadas()
    {
        var ranking = ReportCalculator.Ranking(Rows, top: 10);

        // Las dos tienen 2 solicitudes y 1 aprobada; a igualdad, por nombre.
        Assert.Equal(new[] { 7, 8 }, ranking.Select(r => r.InstallerCompanyId));
        Assert.Equal(new[] { 1, 2 }, ranking.Select(r => r.Rank));
        Assert.Equal(50m, ranking[0].ApprovalRatePct);
        Assert.Equal(100m, ranking[1].ApprovalRatePct); // 1 aprobada, 0 rechazadas
        Assert.Single(ReportCalculator.Ranking(Rows, top: 1));
    }

    private static RequestReport Report(IReadOnlyList<RequestReportRow>? rows = null)
    {
        rows ??= Rows;
        var from = March.AddDays(-9);
        var to = March.AddMonths(2);
        return new RequestReport(ReportCalculator.Statistics(rows, from, to), ReportCalculator.Ranking(rows, 20), rows, March);
    }

    [Fact]
    public void El_Excel_tiene_las_hojas_y_el_detalle_de_cada_solicitud()
    {
        var bytes = new XlsxReportExporter().Export(Report());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(new[] { "Resumen", "Por mes", "Por estado", "Por zona", "Instaladoras", "Detalle" }, workbook.Worksheets.Select(w => w.Name));

        var detail = workbook.Worksheet("Detalle");
        Assert.Equal(Rows.Count + 1, detail.LastRowUsed()!.RowNumber());
        Assert.Equal("REQ-1", detail.Cell(2, 1).GetString());
        Assert.Equal("Aprobada", detail.Cell(2, 3).GetString());
        Assert.Equal(6, workbook.Worksheet("Resumen").Cell(5, 2).GetValue<int>());
    }

    [Fact]
    public void El_Excel_no_convierte_textos_en_formulas()
    {
        var malicious = new[] { Row(1, RequestStatus.Received, March, code: "=HYPERLINK(\"http://x\",\"clic\")") };

        using var workbook = new XLWorkbook(new MemoryStream(new XlsxReportExporter().Export(Report(malicious))));
        var cell = workbook.Worksheet("Detalle").Cell(2, 1);

        Assert.False(cell.HasFormula);
        Assert.StartsWith("=HYPERLINK", cell.GetString());
    }

    [Fact]
    public void El_PDF_se_genera_incluso_sin_datos()
    {
        foreach (var report in new[] { Report(), Report(Array.Empty<RequestReportRow>()) })
        {
            var pdf = new PdfReportExporter().Export(report);

            var output = Environment.GetEnvironmentVariable("ENERGYMAP_REPORT_OUT");
            if (!string.IsNullOrWhiteSpace(output) && report.Rows.Count > 0) File.WriteAllBytes(output, pdf);

            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        }
    }

    [Fact]
    public void Sin_KeyVault_Uri_no_se_agrega_Key_Vault()
    {
        var configuration = new ConfigurationManager();
        var sources = configuration.Sources.Count;

        Assert.False(configuration.AddKeyVaultIfConfigured());
        Assert.Equal(sources, configuration.Sources.Count);
    }
}
