using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIGENRD.Core.Application.Features.Simulations;

namespace SIGENRD.Infrastructure.Shared.Services
{
    /// <summary>Resumen en PDF de una simulación de medición neta (CA-10).</summary>
    public class SimulationReportGenerator : ISimulationReportGenerator
    {
        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");
        private const string Accent = "#0F5132";

        static SimulationReportGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string Money(decimal value) => "RD$" + value.ToString("N2", Es);
        private static string Number(decimal value, int decimals = 2) => value.ToString("N" + decimals, Es);

        private static string Viability(string status) => status switch
        {
            "Profitable" => "Rentable",
            "ConditionallyProfitable" => "Condicionalmente rentable",
            _ => "No rentable"
        };

        private static string TransformerStatus(string status) => status switch
        {
            "Available" => "disponible",
            "Conditioned" => "condicionado",
            "Critical" => "crítico",
            "Saturated" => "saturado",
            "Maintenance" => "en mantenimiento",
            _ => status
        };

        private static string ViabilityColor(string status) => status switch
        {
            "Profitable" => Colors.Green.Darken2,
            "ConditionallyProfitable" => Colors.Orange.Darken2,
            _ => Colors.Red.Darken2
        };

        public byte[] Generate(SimulationDto s)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("EnergyMap").FontSize(9).FontColor(Colors.Grey.Darken1);
                        header.Item().PaddingTop(2).Text("Simulación de medición neta").FontSize(20).Bold().FontColor(Accent);
                        header.Item().Text((s.Name ?? "Simulación " + s.Id) + " · " + s.CreatedAt.ToString("d 'de' MMMM 'de' yyyy", Es))
                            .FontSize(11).FontColor(Colors.Grey.Darken2);
                        header.Item().PaddingTop(8).LineHorizontal(1).LineColor(Accent);
                    });

                    page.Content().PaddingVertical(14).Column(content =>
                    {
                        content.Spacing(14);

                        // Resultado principal
                        content.Item().Row(row =>
                        {
                            void Kpi(string label, string value, string? color = null) =>
                                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                                {
                                    c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text(value).FontSize(13).Bold().FontColor(color ?? Colors.Black);
                                });

                            Kpi("Resultado", Viability(s.ViabilityStatus), ViabilityColor(s.ViabilityStatus));
                            row.Spacing(8);
                            Kpi("Ahorro anual", Money(s.AnnualSavings));
                            Kpi("Retorno", s.PaybackYears is null ? "Sin ahorro" : Number(s.PaybackYears.Value, 1) + " años");
                            Kpi("ROI (vida útil)", Number(s.Roi, 1) + " %");
                        });

                        content.Item().Text("Parámetros").FontSize(12).Bold();
                        content.Item().Element(c => Table(c, new (string, string)[]
                        {
                            ("Consumo mensual", Number(s.MonthlyConsumptionKwh, 0) + " kWh"),
                            ("Potencia instalada", Number(s.InstalledCapacityKw) + " kWp"),
                            ("Inversión", Money(s.InvestmentCost)),
                            ("Tarifa", s.FlatPricePerKwh is null ? "BTS-1 (escalonada)" : Money(s.FlatPricePerKwh.Value) + " por kWh"),
                            ("Ubicación", s.Latitude is null ? "—" : $"{s.Latitude:0.0000}, {s.Longitude:0.0000}"),
                            ("Transformador de referencia", s.ReferenceTransformer is null
                                ? "Ninguno en el radio de búsqueda"
                                : $"{s.ReferenceTransformer.Code} a {Number((decimal)s.ReferenceTransformer.DistanceMeters, 0)} m ({TransformerStatus(s.ReferenceTransformer.Status)})")
                        }));

                        content.Item().Text("Resultados").FontSize(12).Bold();
                        content.Item().Element(c => Table(c, new (string, string)[]
                        {
                            ("Producción anual", Number(s.AnnualProductionKwh, 0) + " kWh"),
                            ("Consumo anual", Number(s.AnnualConsumptionKwh, 0) + " kWh"),
                            ("Cobertura del consumo", Number(s.CoveragePct, 0) + " %"),
                            ("Factura de energía sin paneles", Money(s.AnnualBillBefore) + " al año"),
                            ("Factura de energía con paneles", Money(s.AnnualBillAfter) + " al año"),
                            ("Crédito sobrante a diciembre", s.AnnualExportCreditKwh <= 0
                                ? "Sin excedente"
                                : Number(s.AnnualExportCreditKwh, 0) + " kWh, pagado en " + Money(s.SurplusPayment))
                        }));

                        if (s.Recommendations.Count > 0)
                        {
                            content.Item().Text("Recomendaciones").FontSize(12).Bold();
                            content.Item().Column(list =>
                            {
                                list.Spacing(4);
                                foreach (var recommendation in s.Recommendations)
                                    list.Item().Row(r =>
                                    {
                                        r.ConstantItem(12).Text("•");
                                        r.RelativeItem().Text(recommendation);
                                    });
                            });
                        }

                        content.Item().PaddingTop(6).Text(s.Disclaimer + " Fórmula " + s.FormulaVersion + ".")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                        text.Span("EnergyMap · simulación " + s.Id + " · página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void Table(IContainer container, (string Label, string Value)[] rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(190);
                    columns.RelativeColumn();
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4)
                        .Text(label).FontColor(Colors.Grey.Darken2);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4)
                        .Text(value).SemiBold();
                }
            });
        }
    }
}
