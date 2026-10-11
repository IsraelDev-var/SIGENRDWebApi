using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIGENRD.Core.Application.Features.Reports;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Infrastructure.Shared.Services
{
    internal static class ReportLabels
    {
        public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

        public static string Status(string status) => status switch
        {
            nameof(RequestStatus.Received) => "Recibida",
            nameof(RequestStatus.UnderReview) => "En revisión",
            nameof(RequestStatus.Observed) => "Observaciones",
            nameof(RequestStatus.InCorrection) => "En corrección",
            nameof(RequestStatus.Approved) => "Aprobada",
            nameof(RequestStatus.Rejected) => "Rechazada",
            nameof(RequestStatus.Cancelled) => "Cancelada",
            _ => status
        };

        public static string Type(string type) => type switch
        {
            nameof(RequestType.Connection) => "Conexión",
            nameof(RequestType.NetMetering) => "Medición neta",
            _ => type
        };

        public static string Period(RequestStatisticsDto s) =>
            s.From.ToString("d 'de' MMMM 'de' yyyy", Es) + " al " + s.To.AddDays(-1).ToString("d 'de' MMMM 'de' yyyy", Es);

        public static string Optional(decimal? value, string suffix) => value is null ? "—" : value.Value.ToString("N1", Es) + suffix;
    }

    /// <summary>Reporte en Excel (RF-30): resumen, por mes, por zona, ranking de instaladoras y detalle.</summary>
    public class XlsxReportExporter : IReportExporter
    {
        public ReportFormat Format => ReportFormat.Xlsx;
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string Extension => ".xlsx";

        public byte[] Export(RequestReport report)
        {
            var s = report.Statistics;
            using var workbook = new XLWorkbook();

            // Los textos se escriben como valores, nunca como fórmulas: un "=..." en una dirección no se ejecuta.
            var summary = workbook.Worksheets.Add("Resumen");
            summary.Cell(1, 1).Value = "Reporte de solicitudes — EnergyMap";
            summary.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            summary.Cell(2, 1).Value = "Período: " + ReportLabels.Period(s);
            summary.Cell(3, 1).Value = "Generado: " + report.GeneratedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", ReportLabels.Es);

            var rows = new (string Label, XLCellValue Value)[]
            {
                ("Solicitudes", s.Total),
                ("Pendientes", s.Pending),
                ("Aprobadas", s.Approved),
                ("Rechazadas", s.Rejected),
                ("Canceladas", s.Cancelled),
                ("Tasa de aprobación (%)", s.ApprovalRatePct is null ? Blank.Value : s.ApprovalRatePct.Value),
                ("kVA solicitados", s.RequestedKva),
                ("kVA aprobados", s.ApprovedKva),
                ("Días hasta la primera revisión (promedio)", s.AvgDaysToFirstReview is null ? Blank.Value : s.AvgDaysToFirstReview.Value),
                ("Días hasta la resolución (promedio)", s.AvgDaysToResolution is null ? Blank.Value : s.AvgDaysToResolution.Value),
                ("Días hasta la resolución (mediana)", s.MedianDaysToResolution is null ? Blank.Value : s.MedianDaysToResolution.Value)
            };
            for (var i = 0; i < rows.Length; i++)
            {
                summary.Cell(5 + i, 1).Value = rows[i].Label;
                summary.Cell(5 + i, 2).Value = rows[i].Value;
            }
            summary.Columns().AdjustToContents();

            Table(workbook, "Por mes", new[] { "Mes", "Solicitudes", "Aprobadas", "Rechazadas" },
                s.ByMonth.Select(m => new XLCellValue[] { m.Month, m.Total, m.Approved, m.Rejected }));
            Table(workbook, "Por estado", new[] { "Estado", "Solicitudes" },
                s.ByStatus.Select(c => new XLCellValue[] { ReportLabels.Status(c.Key), c.Count }));
            Table(workbook, "Por zona", new[] { "Zona", "Solicitudes" },
                s.ByZone.Select(c => new XLCellValue[] { c.Key, c.Count }));
            Table(workbook, "Instaladoras", new[] { "Puesto", "Instaladora", "Solicitudes", "Aprobadas", "Rechazadas", "Aprobación (%)", "kVA" },
                report.Ranking.Select(r => new XLCellValue[]
                {
                    r.Rank, r.TradeName, r.Total, r.Approved, r.Rejected,
                    r.ApprovalRatePct is null ? Blank.Value : r.ApprovalRatePct.Value, r.RequestedKva
                }));
            Table(workbook, "Detalle", new[] { "Expediente", "Tipo", "Estado", "Presentada", "Primera revisión", "Resolución", "kVA", "Distribuidora", "Zona", "Instaladora" },
                report.Rows.Select(r => new XLCellValue[]
                {
                    r.RequestCode, ReportLabels.Type(r.RequestType.ToString()), ReportLabels.Status(r.Status.ToString()),
                    r.RequestedAt, r.FirstReviewAt is null ? Blank.Value : r.FirstReviewAt.Value,
                    r.ResolvedAt is null ? Blank.Value : r.ResolvedAt.Value,
                    r.RequestedCapacityKva, r.DistributorName, r.ZoneName, r.InstallerName ?? string.Empty
                }));

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void Table(XLWorkbook workbook, string name, string[] headers, IEnumerable<XLCellValue[]> rows)
        {
            var sheet = workbook.Worksheets.Add(name);
            for (var c = 0; c < headers.Length; c++)
                sheet.Cell(1, c + 1).Value = headers[c];
            sheet.Row(1).Style.Font.SetBold();

            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < row.Length; c++)
                {
                    var cell = sheet.Cell(r, c + 1);
                    cell.Value = row[c];
                    if (row[c].IsDateTime) cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                }
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }
    }

    /// <summary>Reporte en PDF (RF-30): resumen, evolución mensual, zonas y ranking. El detalle va solo en Excel.</summary>
    public class PdfReportExporter : IReportExporter
    {
        private const string Accent = "#0F5132";

        static PdfReportExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ReportFormat Format => ReportFormat.Pdf;
        public string ContentType => "application/pdf";
        public string Extension => ".pdf";

        public byte[] Export(RequestReport report)
        {
            var s = report.Statistics;
            var es = ReportLabels.Es;

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(1.6f, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(9.5f));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("EnergyMap").FontSize(9).FontColor(Colors.Grey.Darken1);
                        header.Item().Text("Reporte de solicitudes").FontSize(18).Bold().FontColor(Accent);
                        header.Item().Text(ReportLabels.Period(s)).FontSize(10).FontColor(Colors.Grey.Darken2);
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Accent);
                    });

                    page.Content().PaddingVertical(12).Column(content =>
                    {
                        content.Spacing(12);

                        content.Item().Row(row =>
                        {
                            row.Spacing(6);
                            void Kpi(string label, string value) =>
                                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
                                {
                                    c.Item().Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text(value).FontSize(12).Bold();
                                });

                            Kpi("Solicitudes", s.Total.ToString("N0", es));
                            Kpi("Pendientes", s.Pending.ToString("N0", es));
                            Kpi("Aprobación", ReportLabels.Optional(s.ApprovalRatePct, " %"));
                            Kpi("Días a la resolución", ReportLabels.Optional(s.MedianDaysToResolution, " (mediana)"));
                        });

                        content.Item().Text("Por mes").FontSize(11).Bold();
                        content.Item().Element(c => Table(c, new[] { "Mes", "Solicitudes", "Aprobadas", "Rechazadas" },
                            s.ByMonth.Select(m => new[] { m.Month, m.Total.ToString("N0", es), m.Approved.ToString("N0", es), m.Rejected.ToString("N0", es) })));

                        content.Item().Text("Por estado").FontSize(11).Bold();
                        content.Item().Element(c => Table(c, new[] { "Estado", "Solicitudes" },
                            s.ByStatus.Select(x => new[] { ReportLabels.Status(x.Key), x.Count.ToString("N0", es) })));

                        content.Item().Text("Por zona").FontSize(11).Bold();
                        content.Item().Element(c => Table(c, new[] { "Zona", "Solicitudes" },
                            s.ByZone.Select(x => new[] { x.Key, x.Count.ToString("N0", es) })));

                        content.Item().Text("Instaladoras más activas").FontSize(11).Bold();
                        content.Item().Element(c => Table(c, new[] { "#", "Instaladora", "Solicitudes", "Aprobadas", "Aprobación" },
                            report.Ranking.Select(r => new[]
                            {
                                r.Rank.ToString(es), r.TradeName, r.Total.ToString("N0", es), r.Approved.ToString("N0", es),
                                ReportLabels.Optional(r.ApprovalRatePct, " %")
                            }), new[] { 0.4f, 3f, 1f, 1f, 1f }));

                        content.Item().Text($"Tiempos: {ReportLabels.Optional(s.AvgDaysToFirstReview, " días")} hasta la primera revisión y " +
                                            $"{ReportLabels.Optional(s.AvgDaysToResolution, " días")} hasta la resolución, en promedio. " +
                                            "El detalle por solicitud está en el reporte en Excel.")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                        text.Span("EnergyMap · generado " + report.GeneratedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", es) + " · página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        /// <param name="widths">Peso relativo de cada columna; por defecto la primera el doble que las demás.</param>
        private static void Table(IContainer container, string[] headers, IEnumerable<string[]> rows, float[]? widths = null)
        {
            var list = rows.ToList();
            widths ??= headers.Select((_, i) => i == 0 ? 2f : 1f).ToArray();
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var width in widths) columns.RelativeColumn(width);
                });

                foreach (var header in headers)
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).Text(header).SemiBold();

                if (list.Count == 0)
                {
                    table.Cell().ColumnSpan((uint)headers.Length).PaddingVertical(3).Text("Sin datos en el período").FontColor(Colors.Grey.Darken1);
                    return;
                }

                foreach (var row in list)
                    foreach (var value in row)
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(value);
            });
        }
    }
}
