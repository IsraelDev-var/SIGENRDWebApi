using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIGENRD.Core.Application.Interfaces.Services;

namespace SIGENRD.Infrastructure.Shared.Services
{
    /// <summary>Certificado de aprobación en PDF (RN-14) con QuestPDF (licencia Community).</summary>
    public class ApprovalCertificateGenerator : ICertificateGenerator
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-DO");
        private static readonly string Accent = "#0F5132";

        static ApprovalCertificateGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(ApprovalCertificateData data)
        {
            var approvedAt = data.ApprovedAt.ToString("d 'de' MMMM 'de' yyyy, HH:mm 'UTC'", Spanish);

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(11));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("EnergyMap").FontSize(10).FontColor(Colors.Grey.Darken1);
                        header.Item().PaddingTop(4).Text("Certificado de aprobación").FontSize(22).Bold().FontColor(Accent);
                        header.Item().Text("Solicitud de " + data.RequestType.ToLower(Spanish) + " · " + data.DistributorName)
                            .FontSize(12).FontColor(Colors.Grey.Darken2);
                        header.Item().PaddingTop(10).LineHorizontal(1).LineColor(Accent);
                    });

                    page.Content().PaddingVertical(20).Column(content =>
                    {
                        content.Spacing(14);

                        content.Item().Text(text =>
                        {
                            text.Span("Se certifica que la solicitud ");
                            text.Span(data.RequestCode).Bold();
                            text.Span(" fue aprobada por " + data.DistributorName + " con la capacidad indicada abajo, " +
                                      "tras verificar la documentación requerida y la capacidad disponible del transformador.");
                        });

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(170);
                                columns.RelativeColumn();
                            });

                            void Row(string label, string value)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6)
                                    .Text(label).FontColor(Colors.Grey.Darken2);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6)
                                    .Text(value).SemiBold();
                            }

                            Row("Número de expediente", data.RequestCode);
                            Row("Solicitante", data.ApplicantName);
                            Row("Dirección del proyecto", string.IsNullOrWhiteSpace(data.ProjectAddress) ? "—" : data.ProjectAddress!);
                            Row("Transformador", data.TransformerCode);
                            Row("Capacidad aprobada", data.ApprovedCapacityKva.ToString("N2", Spanish) + " kVA");
                            Row("Fecha de aprobación", approvedAt);
                            Row("Aprobado por", data.ReviewerName);
                        });

                        content.Item().PaddingTop(10).Text(
                            "Este certificado se emite electrónicamente y forma parte del expediente de la solicitud. " +
                            "Su validez puede confirmarse consultando el expediente en EnergyMap con el número indicado.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken1));
                        text.Span("EnergyMap · " + data.RequestCode + " · página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}
