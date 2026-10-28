using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;
using SIGENRD.Infrastructure.Persistences.Repositories;
using SIGENRD.UnitTests.Application;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Fase 4 de punta a punta: carga, consulta, descarga y baja de documentos (RF-16, RF-34 a RF-37, RN-18, RN-19, RN-22),
/// certificado al aprobar (RN-14, RF-17) y consulta de la auditoría (RN-20).
/// </summary>
[Collection(ApiCollection.Name)]
public class DocumentsTests(ApiFactory api)
{
    private sealed record Seeded(int RequestId, string Code);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Una solicitud Recibida del Customer de prueba, sin documentos.</summary>
    private async Task<Seeded> SeedAsync()
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);
        await using var context = api.NewAppContext();

        var distributor = await context.Distributors.FirstOrDefaultAsync();
        if (distributor is null)
        {
            distributor = new Distributor { Name = "Edesur" };
            context.Distributors.Add(distributor);
        }
        var zone = new ServiceZone { ZoneName = "Zona doc " + Unique() };
        context.ServiceZones.Add(zone);
        await context.SaveChangesAsync();

        var transformer = new Transformer
        {
            Code = "TR-DOC-" + Unique(),
            DistributorId = distributor.Id,
            ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.45, -69.93)
        };
        transformer.UpdateCapacity(1000, 800, 70, 30);
        context.Transformers.Add(transformer);

        var customer = await TestData.CustomerForAsync(context, customerUser.UserId, email: "doc-" + Unique() + "@test.energymap.local");
        await context.SaveChangesAsync();

        var request = new ConnectionRequest
        {
            RequestCode = "REQ-DOC-" + Unique(),
            RequestType = RequestType.Connection,
            CustomerId = customer.Id,
            DistributorId = distributor.Id,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 8,
            ProjectAddress = "Calle Documento 1"
        };
        context.ConnectionRequests.Add(request);
        await context.SaveChangesAsync();

        return new Seeded(request.Id, request.RequestCode);
    }

    private static MultipartFormDataContent Form(byte[] content, string fileName, DocumentType type)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); // el tipo lo decide el contenido
        return new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent(((int)type).ToString()), "documentType" }
        };
    }

    private async Task<HttpResponseMessage> Upload(TestRole role, int requestId, byte[] content, string fileName, DocumentType type)
        => await (await api.ClientFor(role)).PostAsync("/api/v1/ConnectionRequest/" + requestId + "/documents", Form(content, fileName, type));

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, response.StatusCode + ": " + text);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("data").Clone();
    }

    // ---------------- subir, listar, descargar ----------------

    [FactIfDatabase]
    public async Task El_solicitante_sube_lista_y_descarga_su_documento()
    {
        var seeded = await SeedAsync();
        var pdf = DocumentComponentsTests.Pdf("/Producer (prueba)");

        var uploaded = await Data(await Upload(TestRole.Customer, seeded.RequestId, pdf, "cédula escaneada.pdf", DocumentType.IdentityDocument));
        var documentId = uploaded.GetProperty("id").GetInt32();
        Assert.Equal("application/pdf", uploaded.GetProperty("contentType").GetString());
        Assert.Equal(pdf.Length, uploaded.GetProperty("sizeBytes").GetInt64());

        var customer = await api.ClientFor(TestRole.Customer);
        var list = await Data(await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents"));
        Assert.Contains(list.EnumerateArray(), d => d.GetProperty("id").GetInt32() == documentId && d.GetProperty("documentType").GetString() == "IdentityDocument");

        var download = await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId + "/content");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());

        // El personal de la distribuidora también lo ve.
        var engineer = await api.ClientFor(TestRole.Engineer);
        Assert.Equal(HttpStatusCode.OK,
            (await engineer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId + "/content")).StatusCode);
    }

    [FactIfDatabase]
    public async Task Otro_solicitante_no_puede_ver_ni_descargar_ni_subir()
    {
        var seeded = await SeedAsync();
        var documentId = (await Data(await Upload(TestRole.Customer, seeded.RequestId, DocumentComponentsTests.Pdf(), "a.pdf", DocumentType.IdentityDocument)))
            .GetProperty("id").GetInt32();

        // La instaladora de prueba no participa en esta solicitud: responde como si no existiera (RN-22).
        var installer = await api.ClientFor(TestRole.InstallerCompany);
        Assert.Equal(HttpStatusCode.NotFound, (await installer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await installer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId + "/content")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Upload(TestRole.InstallerCompany, seeded.RequestId, DocumentComponentsTests.Pdf(), "b.pdf", DocumentType.Other)).StatusCode);

        // Anónimo: 401. El personal, incluido el administrador, no sube ni retira documentos (matriz 3.2): 403.
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await api.CreateClient().GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents")).StatusCode);
        foreach (var role in new[] { TestRole.Engineer, TestRole.Distributor, TestRole.Admin })
        {
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Upload(role, seeded.RequestId, DocumentComponentsTests.Pdf(), "c.pdf", DocumentType.Other)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await (await api.ClientFor(role)).DeleteAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId)).StatusCode);
        }
    }

    [TheoryIfDatabase]
    [InlineData("png-disfrazado.pdf", "png", DocumentType.Blueprint)]
    [InlineData("script.pdf", "pdf-js", DocumentType.Blueprint)]
    [InlineData("programa.exe", "exe", DocumentType.Other)]
    [InlineData("certificado.pdf", "pdf", DocumentType.ApprovalCertificate)]
    public async Task Rechaza_archivos_no_permitidos_y_el_certificado_subido_a_mano(string fileName, string kind, DocumentType type)
    {
        var seeded = await SeedAsync();
        var content = kind switch
        {
            "png" => DocumentComponentsTests.Png,
            "pdf-js" => DocumentComponentsTests.Pdf("/OpenAction << /S /JavaScript /JS (app.alert(1)) >>"),
            "exe" => new byte[] { 0x4D, 0x5A, 0x90, 0x00 },
            _ => DocumentComponentsTests.Pdf()
        };

        var response = await Upload(TestRole.Customer, seeded.RequestId, content, fileName, type);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var context = api.NewAppContext();
        Assert.False(await context.TechnicalDocuments.AnyAsync(d => d.ConnectionRequestId == seeded.RequestId));
    }

    [FactIfDatabase]
    public async Task Rechaza_archivos_de_mas_de_10_MB()
    {
        var seeded = await SeedAsync();
        var big = new byte[10 * 1024 * 1024 + 1];
        DocumentComponentsTests.Pdf().CopyTo(big, 0);

        var response = await Upload(TestRole.Customer, seeded.RequestId, big, "grande.pdf", DocumentType.Blueprint);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------- baja y auditoría ----------------

    [FactIfDatabase]
    public async Task Retirar_un_documento_lo_saca_del_expediente_y_queda_auditado()
    {
        var seeded = await SeedAsync();
        var documentId = (await Data(await Upload(TestRole.Customer, seeded.RequestId, DocumentComponentsTests.Pdf(), "a.pdf", DocumentType.IdentityDocument)))
            .GetProperty("id").GetInt32();
        var customer = await api.ClientFor(TestRole.Customer);

        Assert.Equal(HttpStatusCode.OK, (await customer.DeleteAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId)).StatusCode);

        var list = await Data(await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents"));
        Assert.DoesNotContain(list.EnumerateArray(), d => d.GetProperty("id").GetInt32() == documentId);
        Assert.Equal(HttpStatusCode.NotFound,
            (await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + documentId + "/content")).StatusCode);

        // RN-20: la carga y la baja están en la auditoría, que solo consulta el administrador.
        var admin = await api.ClientFor(TestRole.Admin);
        var audit = await Data(await admin.GetAsync("/api/v1/AuditLogs?entityName=TechnicalDocument&entityId=" + documentId));
        var actions = audit.GetProperty("items").EnumerateArray().Select(a => a.GetProperty("action").GetString()).ToList();
        Assert.Contains("Added", actions);
        Assert.Contains("Modified", actions);

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/AuditLogs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await api.ClientFor(TestRole.Distributor)).GetAsync("/api/v1/AuditLogs")).StatusCode);
    }

    [FactIfDatabase]
    public async Task La_consulta_de_auditoria_pagina_y_filtra_inicios_de_sesion()
    {
        var admin = await api.ClientFor(TestRole.Admin);
        await api.CreateClient().PostAsync("/api/v1/Auth/login",
            new StringContent("{\"email\":\"auditoria-" + Unique() + "@test.energymap.local\",\"password\":\"Incorrecta-12345\"}", Encoding.UTF8, "application/json"));

        var page = await Data(await admin.GetAsync("/api/v1/AuditLogs?entityName=Account&action=LoginFailed&pageSize=5"));

        Assert.Equal(5, page.GetProperty("pageSize").GetInt32());
        Assert.InRange(page.GetProperty("items").GetArrayLength(), 1, 5);
        Assert.All(page.GetProperty("items").EnumerateArray(), a => Assert.Equal("LoginFailed", a.GetProperty("action").GetString()));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/AuditLogs?pageSize=101")).StatusCode);
    }

    // ---------------- flujo completo con certificado ----------------

    [FactIfDatabase]
    public async Task Con_los_documentos_subidos_se_aprueba_se_emite_el_certificado_y_los_documentos_quedan_fijos()
    {
        var seeded = await SeedAsync();

        // Un documento por requisito, subido por la API.
        await using (var context = api.NewAppContext())
        {
            var request = await context.ConnectionRequests.Include(r => r.Customer).SingleAsync(r => r.Id == seeded.RequestId);
            foreach (var requirement in RequiredDocuments.For(request, request.Customer!.IsLegalEntity))
                await Data(await Upload(TestRole.Customer, seeded.RequestId, DocumentComponentsTests.Pdf(), requirement.Code + ".pdf", requirement.AnyOf[0]));
        }

        var customer = await api.ClientFor(TestRole.Customer);
        var required = await Data(await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/required-documents"));
        Assert.True(required.GetProperty("complete").GetBoolean());

        var distributor = await api.ClientFor(TestRole.Distributor);
        Assert.Equal(HttpStatusCode.OK, (await distributor.PostAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/start-review", null)).StatusCode);

        // En revisión no se cambian documentos (409).
        Assert.Equal(HttpStatusCode.Conflict,
            (await Upload(TestRole.Customer, seeded.RequestId, DocumentComponentsTests.Pdf(), "tarde.pdf", DocumentType.Other)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await distributor.PostAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/approve", null)).StatusCode);

        // RN-14: el certificado está en el expediente y se descarga.
        var certificate = await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/certificate");
        Assert.Equal(HttpStatusCode.OK, certificate.StatusCode);
        Assert.Equal("application/pdf", certificate.Content.Headers.ContentType!.MediaType);
        var pdf = await certificate.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Contains(seeded.Code, certificate.Content.Headers.ContentDisposition!.FileNameStar ?? certificate.Content.Headers.ContentDisposition.FileName);

        var documents = await Data(await customer.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents"));
        var certificateDoc = documents.EnumerateArray().Single(d => d.GetProperty("documentType").GetString() == "ApprovalCertificate");
        var anyUploaded = documents.EnumerateArray().First(d => d.GetProperty("documentType").GetString() != "ApprovalCertificate");

        // RN-19: tras la aprobación nada se sube ni se retira; el certificado nunca se retira.
        Assert.Equal(HttpStatusCode.Conflict,
            (await Upload(TestRole.Customer, seeded.RequestId, DocumentComponentsTests.Pdf(), "despues.pdf", DocumentType.Other)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await customer.DeleteAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + anyUploaded.GetProperty("id").GetInt32())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await customer.DeleteAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/" + certificateDoc.GetProperty("id").GetInt32())).StatusCode);

        // El aviso al solicitante menciona el certificado.
        await using var check = api.NewAppContext();
        Assert.True(await check.Notifications.AnyAsync(n =>
            n.NotificationType == NotificationType.RequestApproved && n.Message.Contains(seeded.Code) && n.Message.Contains("certificado")));
    }

    [FactIfDatabase]
    public async Task Una_solicitud_sin_certificado_responde_404()
    {
        var seeded = await SeedAsync();

        Assert.Equal(HttpStatusCode.NotFound,
            (await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/certificate")).StatusCode);
    }

    // ---------------- subida directa con permiso (Vercel no acepta cuerpos de 10 MB) ----------------

    private async Task<HttpResponseMessage> RequestTicket(TestRole role, int requestId, DocumentType type)
        => await (await api.ClientFor(role)).PostAsync("/api/v1/ConnectionRequest/" + requestId + "/documents/upload-ticket",
            new StringContent("{\"documentType\":\"" + type + "\"}", Encoding.UTF8, "application/json"));

    private async Task<HttpResponseMessage> UploadWithTicket(string ticket, string path, byte[] content, string fileName, DocumentType type)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ticket);
        return await client.PostAsync(path, Form(content, fileName, type));
    }

    [FactIfDatabase]
    public async Task El_solicitante_sube_directo_con_un_permiso_de_un_solo_uso()
    {
        var seeded = await SeedAsync();
        var ticket = await Data(await RequestTicket(TestRole.Customer, seeded.RequestId, DocumentType.IdentityDocument));
        var token = ticket.GetProperty("ticket").GetString()!;
        var path = ticket.GetProperty("uploadPath").GetString()!;
        Assert.Equal("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/direct", path);
        Assert.True(ticket.GetProperty("expiresAt").GetDateTime() <= DateTime.UtcNow.AddMinutes(5.1));

        var pdf = DocumentComponentsTests.Pdf();
        var uploaded = await Data(await UploadWithTicket(token, path, pdf, "cédula.pdf", DocumentType.IdentityDocument));
        Assert.Equal("IdentityDocument", uploaded.GetProperty("documentType").GetString());
        Assert.Equal(pdf.Length, uploaded.GetProperty("sizeBytes").GetInt64());

        // Queda a nombre del solicitante: lo ve en su expediente.
        var list = await Data(await (await api.ClientFor(TestRole.Customer)).GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents"));
        Assert.Contains(list.EnumerateArray(), d => d.GetProperty("id").GetInt32() == uploaded.GetProperty("id").GetInt32());

        // El mismo permiso no sirve dos veces.
        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadWithTicket(token, path, pdf, "otra.pdf", DocumentType.IdentityDocument)).StatusCode);
    }

    [FactIfDatabase]
    public async Task El_permiso_solo_vale_para_su_solicitud_y_su_tipo()
    {
        var seeded = await SeedAsync();
        var other = await SeedAsync();
        var pdf = DocumentComponentsTests.Pdf();

        var forType = await Data(await RequestTicket(TestRole.Customer, seeded.RequestId, DocumentType.IdentityDocument));
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadWithTicket(forType.GetProperty("ticket").GetString()!,
            "/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/direct", pdf, "a.pdf", DocumentType.Other)).StatusCode);

        var forRequest = await Data(await RequestTicket(TestRole.Customer, seeded.RequestId, DocumentType.IdentityDocument));
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadWithTicket(forRequest.GetProperty("ticket").GetString()!,
            "/api/v1/ConnectionRequest/" + other.RequestId + "/documents/direct", pdf, "a.pdf", DocumentType.IdentityDocument)).StatusCode);
    }

    [FactIfDatabase]
    public async Task El_permiso_y_la_sesion_no_se_intercambian()
    {
        var seeded = await SeedAsync();
        var path = "/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/direct";

        // El token de sesión no sirve para la subida directa.
        var session = await api.LoginAsync(TestRole.Customer);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await UploadWithTicket(session.Token, path, DocumentComponentsTests.Pdf(), "a.pdf", DocumentType.IdentityDocument)).StatusCode);

        // Ni el permiso sirve como sesión en otra ruta.
        var ticket = (await Data(await RequestTicket(TestRole.Customer, seeded.RequestId, DocumentType.IdentityDocument))).GetProperty("ticket").GetString()!;
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ticket);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents",
            Form(DocumentComponentsTests.Pdf(), "a.pdf", DocumentType.IdentityDocument))).StatusCode);
    }

    [FactIfDatabase]
    public async Task Sin_derecho_sobre_la_solicitud_no_se_emite_permiso()
    {
        var seeded = await SeedAsync();

        // Solicitud ajena: como si no existiera (RN-22). Personal: 403 (matriz 3.2). Certificado: lo genera el sistema.
        Assert.Equal(HttpStatusCode.NotFound, (await RequestTicket(TestRole.InstallerCompany, seeded.RequestId, DocumentType.Other)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RequestTicket(TestRole.Engineer, seeded.RequestId, DocumentType.Other)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RequestTicket(TestRole.Customer, seeded.RequestId, DocumentType.ApprovalCertificate)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await api.CreateClient().PostAsync("/api/v1/ConnectionRequest/" + seeded.RequestId + "/documents/upload-ticket",
                new StringContent("{\"documentType\":\"Other\"}", Encoding.UTF8, "application/json"))).StatusCode);
    }
}
