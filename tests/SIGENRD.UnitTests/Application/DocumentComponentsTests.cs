using System.Text;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Application.Services;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Application;

/// <summary>Piezas de la gestión documental que no necesitan base de datos (RN-14, RN-18, RF-36).</summary>
public class DocumentComponentsTests
{
    public static byte[] Pdf(string body = "") =>
        Encoding.Latin1.GetBytes("%PDF-1.4\n1 0 obj << /Type /Catalog " + body + " >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF\n");

    public static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52 };
    public static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 16, 0x4A, 0x46, 0x49, 0x46 };

    // ---------------- inspector de archivos ----------------

    [Theory]
    [InlineData("plano.pdf", "pdf", "application/pdf", ".pdf")]
    [InlineData("foto.PNG", "png", "image/png", ".png")]
    [InlineData("cedula.jpeg", "jpg", "image/jpeg", ".jpg")]
    [InlineData("cedula.jpg", "jpg", "image/jpeg", ".jpg")]
    public void Acepta_PDF_PNG_y_JPG_por_su_contenido(string fileName, string kind, string contentType, string extension)
    {
        var content = kind switch { "pdf" => Pdf(), "png" => Png, _ => Jpeg };

        var result = DocumentFileInspector.Inspect(fileName, content);

        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(extension, result.Extension);
    }

    [Theory]
    [InlineData("disfrazado.pdf", "png")]   // la extensión no coincide con el contenido
    [InlineData("programa.pdf", "exe")]     // ejecutable con nombre de PDF
    [InlineData("pagina.html", "html")]
    [InlineData("vacio.pdf", "empty")]
    public void Rechaza_archivos_cuyo_contenido_no_es_PDF_JPG_o_PNG_o_no_coincide(string fileName, string kind)
    {
        var content = kind switch
        {
            "png" => Png,
            "exe" => new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 },
            "html" => Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"),
            _ => Array.Empty<byte>()
        };

        Assert.Throws<ApiException>(() => DocumentFileInspector.Inspect(fileName, content));
    }

    [Theory]
    [InlineData("/OpenAction << /S /JavaScript /JS (app.alert(1)) >>")]
    [InlineData("/AA << /O << /S /Launch /F (cmd.exe) >> >>")]
    [InlineData("/Names << /EmbeddedFiles << >> >> /EmbeddedFile")]
    public void Rechaza_PDF_con_contenido_activo(string body)
    {
        Assert.Throws<ApiException>(() => DocumentFileInspector.Inspect("malicioso.pdf", Pdf(body)));
    }

    [Fact]
    public void Acepta_un_PDF_que_solo_fija_la_vista_inicial()
    {
        var result = DocumentFileInspector.Inspect("normal.pdf", Pdf("/OpenAction [3 0 R /Fit] /JSomething 1"));
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public void Rechaza_archivos_de_mas_de_10_MB()
    {
        var content = new byte[DocumentFileInspector.MaxSizeBytes + 1];
        Pdf().CopyTo(content, 0);

        Assert.Throws<ApiException>(() => DocumentFileInspector.Inspect("grande.pdf", content));
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("C:\\Users\\x\\plano final.pdf", "plano final.pdf")]
    [InlineData("a\"b<c>|d.pdf", "abcd.pdf")]
    [InlineData(".pdf", "documento.pdf")]
    public void Limpia_el_nombre_del_archivo(string fileName, string expected)
    {
        Assert.Equal(expected, DocumentFileInspector.Inspect(fileName, Pdf()).SafeFileName);
    }

    // ---------------- almacén local ----------------

    private static LocalFileStorage Storage(string root) =>
        new(Microsoft.Extensions.Options.Options.Create(new StorageSettings { Provider = "Local", LocalPath = root }));

    [Fact]
    public async Task El_almacen_local_guarda_lee_y_borra()
    {
        var root = Path.Combine(Path.GetTempPath(), "energymap-storage-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var storage = Storage(root);
            await storage.SaveAsync("requests/1/abc.pdf", new MemoryStream(Pdf()), "application/pdf");

            await using (var read = await storage.OpenReadAsync("requests/1/abc.pdf"))
            {
                using var copy = new MemoryStream();
                await read!.CopyToAsync(copy);
                Assert.Equal(Pdf(), copy.ToArray());
            }

            await storage.DeleteAsync("requests/1/abc.pdf");
            Assert.Null(await storage.OpenReadAsync("requests/1/abc.pdf"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../fuera.pdf")]
    [InlineData("requests/../../fuera.pdf")]
    [InlineData("/absoluta.pdf")]
    [InlineData("C:/Windows/win.ini")]
    public async Task El_almacen_local_rechaza_claves_que_salen_de_su_carpeta(string key)
    {
        var storage = Storage(Path.Combine(Path.GetTempPath(), "energymap-storage-" + Guid.NewGuid().ToString("N")[..8]));

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(key, new MemoryStream(Pdf()), "application/pdf"));
    }

    // ---------------- certificado ----------------

    [Fact]
    public void Genera_el_certificado_en_PDF()
    {
        var pdf = new ApprovalCertificateGenerator().Generate(new ApprovalCertificateData(
            RequestCode: "REQ-1234ABCD",
            RequestType: "Medición neta",
            ApplicantName: "María Pérez",
            ProjectAddress: "Calle 1, Naco, Santo Domingo",
            DistributorName: "Edesur",
            TransformerCode: "TR-0001",
            ApprovedCapacityKva: 12.5m,
            ApprovedAt: new DateTime(2026, 10, 11, 14, 30, 0, DateTimeKind.Utc),
            ReviewerName: "Ana Revisora"));

        // Para revisarlo a ojo: ENERGYMAP_CERT_OUT=/ruta/certificado.pdf
        var output = Environment.GetEnvironmentVariable("ENERGYMAP_CERT_OUT");
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllBytes(output, pdf);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        // Lo que genera el sistema también pasa la inspección de documentos.
        Assert.Equal("application/pdf", DocumentFileInspector.Inspect("Certificado.pdf", pdf).ContentType);
    }
}
