using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Flujo de estados de punta a punta (RN-12, RN-23) con reserva de capacidad (RN-03, RN-15): API real, base real,
/// token real por rol. Los datos se siembran directo en la base porque la carga de documentos llega en la fase 4.
/// </summary>
[Collection(ApiCollection.Name)]
public class RequestWorkflowTests(ApiFactory api)
{
    private sealed record Flow(int RequestId, int TransformerId, string Code, string CustomerEmail);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    /// <summary>Un transformador y una solicitud Recibida con sus documentos mínimos, del Customer de prueba.</summary>
    private async Task<Flow> SeedAsync(
        decimal total = 1000, decimal available = 800, decimal requested = 100,
        RequestType type = RequestType.Connection, bool withDocuments = true, bool withInstaller = false)
    {
        var customerUser = await api.LoginAsync(TestRole.Customer);
        var installerUser = await api.LoginAsync(TestRole.InstallerCompany);

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
            Code = "TR-WF-" + Unique(),
            DistributorId = distributor.Id,
            ServiceZoneId = zone.Id,
            Location = TransformerQueries.ToPoint(18.4, -69.9)
        };
        transformer.UpdateCapacity(total, available, 70, 30);

        var email = "cliente-" + Unique() + "@test.energymap.local";
        var customer = await TestData.CustomerForAsync(context, customerUser.UserId, email: email);
        var company = await TestData.CompanyForAsync(context, installerUser.UserId);
        context.Transformers.Add(transformer);
        await context.SaveChangesAsync();

        var request = new ConnectionRequest
        {
            RequestCode = "REQ-WF-" + Unique(),
            RequestType = type,
            CustomerId = customer.Id,
            InstallerCompanyId = withInstaller ? company.Id : null,
            DistributorId = distributor.Id,
            TransformerId = transformer.Id,
            RequestedCapacityKva = requested
        };
        context.ConnectionRequests.Add(request);
        await context.SaveChangesAsync();

        if (withDocuments)
        {
            foreach (var requirement in RequiredDocuments.For(request, customer.IsLegalEntity))
            {
                var documentType = requirement.AnyOf[0];
                context.TechnicalDocuments.Add(new TechnicalDocument
                {
                    ConnectionRequestId = request.Id,
                    DocumentType = documentType,
                    FileName = documentType + ".pdf",
                    FileUrl = "https://blob.test/" + Unique(),
                    ContentType = "application/pdf",
                    SizeBytes = 1024
                });
            }
            await context.SaveChangesAsync();
        }

        return new Flow(request.Id, transformer.Id, request.RequestCode, email);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> Send(TestRole role, string path, string? json = null, HttpMethod? method = null)
    {
        var client = await api.ClientFor(role);
        var message = new HttpRequestMessage(method ?? HttpMethod.Post, "/api/v1/ConnectionRequest/" + path)
        {
            Content = json is null ? null : Json(json)
        };

        var response = await client.SendAsync(message);
        var text = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        return (response.StatusCode, doc.RootElement.Clone());
    }

    private static string Errors(JsonElement body) =>
        body.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
            ? string.Join(" | ", e.EnumerateArray().Select(x => x.GetString()))
            : body.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";

    private async Task<ConnectionRequest> Reload(int id)
    {
        await using var context = api.NewAppContext();
        return await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == id);
    }

    private async Task<Transformer> ReloadTransformer(int id)
    {
        await using var context = api.NewAppContext();
        return await context.Transformers.AsNoTracking().SingleAsync(t => t.Id == id);
    }

    private async Task<List<string>> HistoryOf(int requestId)
    {
        await using var context = api.NewAppContext();
        return await context.StateHistories.AsNoTracking()
            .Where(h => h.ConnectionRequestId == requestId).OrderBy(h => h.Id)
            .Select(h => h.PreviousState + ">" + h.NewState).ToListAsync();
    }

    // ---------------- flujo completo ----------------

    [FactIfDatabase]
    public async Task Flujo_completo_revisar_observar_corregir_reenviar_y_aprobar()
    {
        var flow = await SeedAsync(total: 1000, available: 800, requested: 300);

        // 1. Revisión: reserva la capacidad.
        var review = await Send(TestRole.Engineer, flow.RequestId + "/start-review");
        Assert.Equal(HttpStatusCode.OK, review.Status);
        var transformer = await ReloadTransformer(flow.TransformerId);
        Assert.Equal(300, transformer.ReservedCapacityKva);
        Assert.Equal(800, transformer.AvailableCapacityKva);

        // 2. Observaciones: el comentario es obligatorio.
        var withoutComment = await Send(TestRole.Engineer, flow.RequestId + "/observe", "{\"comment\":\"\"}");
        Assert.Equal(HttpStatusCode.BadRequest, withoutComment.Status);

        var observe = await Send(TestRole.Engineer, flow.RequestId + "/observe",
            "{\"comment\":\"Falta firmar el plano unifilar\",\"observationType\":2}");
        Assert.Equal(HttpStatusCode.OK, observe.Status);
        Assert.Equal("Observed", observe.Body.GetProperty("data").GetProperty("status").GetString());

        // 3. El solicitante corrige y reenvía.
        Assert.Equal(HttpStatusCode.OK, (await Send(TestRole.Customer, flow.RequestId + "/start-correction")).Status);
        Assert.Equal(HttpStatusCode.OK, (await Send(TestRole.Customer, flow.RequestId + "/resubmit")).Status);
        Assert.Equal(300, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva); // la reserva se conserva

        // 4. Aprobación: la reserva pasa a descontarse de lo disponible.
        var approve = await Send(TestRole.Distributor, flow.RequestId + "/approve");
        Assert.Equal(HttpStatusCode.OK, approve.Status);

        var approved = await Reload(flow.RequestId);
        Assert.Equal(RequestStatus.Approved, approved.Status);
        Assert.Equal(0, approved.ReservedKva);

        transformer = await ReloadTransformer(flow.TransformerId);
        Assert.Equal(0, transformer.ReservedCapacityKva);
        Assert.Equal(500, transformer.AvailableCapacityKva);

        // 5. Rastro: historial, observación y avisos.
        Assert.Equal(
            new[] { "Received>UnderReview", "UnderReview>Observed", "Observed>InCorrection", "InCorrection>UnderReview", "UnderReview>Approved" },
            (await HistoryOf(flow.RequestId)).ToArray());

        await using var context = api.NewAppContext();
        var observation = await context.ReviewObservations.AsNoTracking().SingleAsync(o => o.ConnectionRequestId == flow.RequestId);
        Assert.Equal("Falta firmar el plano unifilar", observation.Comment);
        Assert.Equal(ObservationType.Documental, observation.ObservationType);

        var customer = await api.LoginAsync(TestRole.Customer);
        var notifications = await context.Notifications.AsNoTracking()
            .Where(n => n.UserId == customer.UserId && n.Title.Contains(flow.Code)).ToListAsync();
        Assert.Equal(5, notifications.Count);

        // 6. El correo también sale (RN-17).
        Assert.Contains(api.Emails.Sent, e => e.To == flow.CustomerEmail && e.Subject.Contains("aprobada"));
    }

    [FactIfDatabase]
    public async Task Sin_documentos_minimos_no_pasa_a_revision()
    {
        var flow = await SeedAsync(withDocuments: false, type: RequestType.NetMetering);

        var result = await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Contains("Plano de situación", Errors(result.Body));
        Assert.Equal(RequestStatus.Received, (await Reload(flow.RequestId)).Status);
        Assert.Equal(0, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);
    }

    [FactIfDatabase]
    public async Task Sin_capacidad_no_entra_en_revision()
    {
        var flow = await SeedAsync(total: 1000, available: 100, requested: 500);

        var result = await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.Equal(RequestStatus.Received, (await Reload(flow.RequestId)).Status);
        Assert.Equal(0, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);
    }

    [FactIfDatabase]
    public async Task Rechazar_exige_comentario_y_libera_la_reserva()
    {
        var flow = await SeedAsync(requested: 250);
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        var withoutComment = await Send(TestRole.Distributor, flow.RequestId + "/reject", "{}");
        Assert.Equal(HttpStatusCode.BadRequest, withoutComment.Status);

        var reject = await Send(TestRole.Distributor, flow.RequestId + "/reject", "{\"comment\":\"No cumple la norma de interconexion\"}");
        Assert.Equal(HttpStatusCode.OK, reject.Status);

        Assert.Equal(RequestStatus.Rejected, (await Reload(flow.RequestId)).Status);
        Assert.Equal(0, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);
    }

    // ---------------- cancelación ----------------

    [FactIfDatabase]
    public async Task El_solicitante_cancela_una_solicitud_recibida()
    {
        var flow = await SeedAsync();

        var result = await Send(TestRole.Customer, flow.RequestId + "/cancel", "{\"comment\":\"Ya no la necesito\"}");

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal(RequestStatus.Cancelled, (await Reload(flow.RequestId)).Status);
    }

    [FactIfDatabase]
    public async Task Cancelar_desde_observaciones_libera_la_reserva()
    {
        var flow = await SeedAsync(requested: 200);
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");
        await Send(TestRole.Distributor, flow.RequestId + "/observe", "{\"comment\":\"Falta informacion\"}");
        Assert.Equal(200, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);

        var result = await Send(TestRole.Customer, flow.RequestId + "/cancel");

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal(0, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);
    }

    [FactIfDatabase]
    public async Task No_se_cancela_una_solicitud_en_revision()
    {
        var flow = await SeedAsync();
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        var result = await Send(TestRole.Customer, flow.RequestId + "/cancel");

        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.Equal(RequestStatus.UnderReview, (await Reload(flow.RequestId)).Status);
    }

    [FactIfDatabase]
    public async Task Otro_solicitante_no_puede_cancelar_ni_corregir_una_solicitud_ajena()
    {
        var flow = await SeedAsync(); // sin instaladora: la instaladora de prueba es ajena

        Assert.Equal(HttpStatusCode.NotFound, (await Send(TestRole.InstallerCompany, flow.RequestId + "/cancel")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(TestRole.InstallerCompany, flow.RequestId + "/start-correction")).Status);
        Assert.Equal(RequestStatus.Received, (await Reload(flow.RequestId)).Status);
    }

    // ---------------- capacidad que cae durante la revisión (RN-03) ----------------

    [FactIfDatabase]
    public async Task Si_la_capacidad_cae_la_aprobacion_se_bloquea_y_vuelve_a_observaciones()
    {
        var flow = await SeedAsync(total: 1000, available: 800, requested: 300);
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        // La distribuidora informa que ya solo quedan 200 kVA disponibles.
        var client = await api.ClientFor(TestRole.Distributor);
        var update = await client.PutAsync("/api/v1/Transformers/" + flow.TransformerId + "/capacity",
            Json("{\"totalCapacityKva\":1000,\"availableCapacityKva\":200}"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var approve = await Send(TestRole.Distributor, flow.RequestId + "/approve");

        Assert.Equal(HttpStatusCode.Conflict, approve.Status);
        Assert.Equal(RequestStatus.Observed, (await Reload(flow.RequestId)).Status);
        Assert.Equal(300, (await ReloadTransformer(flow.TransformerId)).ReservedCapacityKva);

        var history = await HistoryOf(flow.RequestId);
        Assert.Equal("UnderReview>Observed", history.Last());
    }

    // ---------------- estados finales y transiciones inválidas ----------------

    [FactIfDatabase]
    public async Task Una_solicitud_aprobada_no_admite_mas_cambios()
    {
        var flow = await SeedAsync();
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");
        await Send(TestRole.Distributor, flow.RequestId + "/approve");

        var attempts = new[]
        {
            await Send(TestRole.Distributor, flow.RequestId + "/observe", "{\"comment\":\"x\"}"),
            await Send(TestRole.Distributor, flow.RequestId + "/reject", "{\"comment\":\"x\"}"),
            await Send(TestRole.Distributor, flow.RequestId + "/start-review"),
            await Send(TestRole.Distributor, flow.RequestId + "/approve"),
            await Send(TestRole.Customer, flow.RequestId + "/cancel"),
            await Send(TestRole.Customer, flow.RequestId + "/start-correction"),
            await Send(TestRole.Customer, flow.RequestId + "/resubmit"),
        };

        Assert.All(attempts, a => Assert.Equal(HttpStatusCode.Conflict, a.Status));
        Assert.Equal(RequestStatus.Approved, (await Reload(flow.RequestId)).Status);
    }

    [FactIfDatabase]
    public async Task No_se_puede_saltar_pasos()
    {
        var flow = await SeedAsync();

        // Recibida no se aprueba, ni se observa, ni se rechaza, ni se reenvía.
        Assert.Equal(HttpStatusCode.Conflict, (await Send(TestRole.Distributor, flow.RequestId + "/approve")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(TestRole.Distributor, flow.RequestId + "/observe", "{\"comment\":\"x\"}")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(TestRole.Distributor, flow.RequestId + "/reject", "{\"comment\":\"x\"}")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(TestRole.Customer, flow.RequestId + "/resubmit")).Status);
        Assert.Equal(RequestStatus.Received, (await Reload(flow.RequestId)).Status);
    }

    // ---------------- roles ----------------

    [TheoryIfDatabase]
    [InlineData(TestRole.Anonymous, "start-review", HttpStatusCode.Unauthorized)]
    [InlineData(TestRole.Customer, "start-review", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.InstallerCompany, "approve", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.Engineer, "approve", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.Engineer, "reject", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.Distributor, "start-correction", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.Distributor, "cancel", HttpStatusCode.Forbidden)]
    [InlineData(TestRole.Admin, "resubmit", HttpStatusCode.Forbidden)]
    public async Task Cada_rol_solo_ejecuta_las_transiciones_que_le_corresponden(TestRole role, string action, HttpStatusCode expected)
    {
        var flow = await SeedAsync();

        var result = await Send(role, flow.RequestId + "/" + action, "{\"comment\":\"x\"}");

        Assert.Equal(expected, result.Status);
        Assert.Equal(RequestStatus.Received, (await Reload(flow.RequestId)).Status);
    }

    // ---------------- concurrencia ----------------

    [FactIfDatabase]
    public async Task Dos_revisores_compitiendo_por_la_ultima_capacidad_solo_uno_la_reserva()
    {
        // Un transformador con 500 kVA libres y dos solicitudes de 400: solo una cabe.
        var first = await SeedAsync(total: 1000, available: 500, requested: 400);
        int secondId;
        await using (var context = api.NewAppContext())
        {
            var template = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == first.RequestId);
            var second = new ConnectionRequest
            {
                RequestCode = "REQ-WF-" + Unique(),
                CustomerId = template.CustomerId,
                DistributorId = template.DistributorId,
                TransformerId = template.TransformerId,
                RequestedCapacityKva = 400
            };
            context.ConnectionRequests.Add(second);
            await context.SaveChangesAsync();

            foreach (var requirement in RequiredDocuments.For(second, false))
                context.TechnicalDocuments.Add(new TechnicalDocument
                {
                    ConnectionRequestId = second.Id, DocumentType = requirement.AnyOf[0], FileName = "d.pdf",
                    FileUrl = "https://blob.test/" + Unique(), ContentType = "application/pdf", SizeBytes = 1
                });
            await context.SaveChangesAsync();
            secondId = second.Id;
        }

        var a = Send(TestRole.Distributor, first.RequestId + "/start-review");
        var b = Send(TestRole.Admin, secondId + "/start-review");
        var results = await Task.WhenAll(a, b);

        var statuses = results.Select(r => r.Status).OrderBy(s => (int)s).ToArray();
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses);

        var transformer = await ReloadTransformer(first.TransformerId);
        Assert.Equal(400, transformer.ReservedCapacityKva);
    }

    // ---------------- creación e historial ----------------

    [FactIfDatabase]
    public async Task Crear_solicitud_registra_el_primer_estado_en_el_historial()
    {
        var seeded = await SeedAsync(withDocuments: false);
        await using var context = api.NewAppContext();
        var template = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == seeded.RequestId);

        var body = "{\"customerId\":" + template.CustomerId + ",\"distributorId\":" + template.DistributorId +
                   ",\"transformerId\":" + template.TransformerId + ",\"requestType\":1,\"requestedCapacityKva\":5," +
                   "\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1,\"projectAddress\":\"Calle 1\"," +
                   "\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";

        var created = await Send(TestRole.Customer, "", body);
        Assert.Equal(HttpStatusCode.OK, created.Status);

        var id = created.Body.GetProperty("data").GetInt32();
        Assert.Equal(new[] { ">Draft" }, (await HistoryOf(id)).ToArray());

        // D-4: al enviarlo con sus documentos pasa a Recibida, y eso también queda en el historial.
        await AddRequiredDocumentsAsync(id);
        var sent = await Send(TestRole.Customer, id + "/submit");
        Assert.Equal(HttpStatusCode.OK, sent.Status);
        Assert.Equal(new[] { ">Draft", "Draft>Received" }, (await HistoryOf(id)).ToArray());
    }

    [FactIfDatabase]
    public async Task No_se_envia_un_borrador_si_el_transformador_dejo_de_admitir_solicitudes()
    {
        var seeded = await SeedAsync(withDocuments: false);
        int transformerId;
        string body;
        await using (var context = api.NewAppContext())
        {
            var template = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == seeded.RequestId);
            transformerId = template.TransformerId;
            body = "{\"distributorId\":" + template.DistributorId +
                   ",\"transformerId\":" + template.TransformerId + ",\"requestType\":1,\"requestedCapacityKva\":5," +
                   "\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1,\"projectAddress\":\"Calle 1\"," +
                   "\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";
        }

        var created = await Send(TestRole.Customer, "", body);
        var id = created.Body.GetProperty("data").GetInt32();
        await AddRequiredDocumentsAsync(id);

        await using (var context = api.NewAppContext())
        {
            var transformer = await context.Transformers.SingleAsync(t => t.Id == transformerId);
            transformer.UpdateCapacity(transformer.TotalCapacityKva, 0, 70, 30);
            await context.SaveChangesAsync();
        }

        var sent = await Send(TestRole.Customer, id + "/submit");
        Assert.Equal(HttpStatusCode.BadRequest, sent.Status);
        Assert.Equal(new[] { ">Draft" }, (await HistoryOf(id)).ToArray());
    }

    private async Task AddRequiredDocumentsAsync(int id)
    {
        await using var context = api.NewAppContext();
        var request = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == id);
        foreach (var requirement in RequiredDocuments.For(request, false))
            context.TechnicalDocuments.Add(new TechnicalDocument
            {
                ConnectionRequestId = id, DocumentType = requirement.AnyOf[0], FileName = "d.pdf",
                FileUrl = "https://blob.test/" + Unique(), ContentType = "application/pdf", SizeBytes = 1
            });
        await context.SaveChangesAsync();
    }

    [FactIfDatabase]
    public async Task No_se_crean_solicitudes_sobre_un_transformador_saturado()
    {
        var seeded = await SeedAsync(total: 1000, available: 0);
        await using var context = api.NewAppContext();
        var template = await context.ConnectionRequests.AsNoTracking().SingleAsync(r => r.Id == seeded.RequestId);

        var body = "{\"customerId\":" + template.CustomerId + ",\"distributorId\":" + template.DistributorId +
                   ",\"transformerId\":" + template.TransformerId + ",\"requestType\":1,\"requestedCapacityKva\":5," +
                   "\"usageType\":1,\"tariffType\":1,\"interconnectionType\":1,\"projectAddress\":\"Calle 1\"," +
                   "\"latitude\":18.4,\"longitude\":-69.9,\"declarationAccepted\":true}";

        var created = await Send(TestRole.Customer, "", body);

        Assert.Equal(HttpStatusCode.BadRequest, created.Status);
        Assert.Contains("Saturated", Errors(created.Body));
    }

    [FactIfDatabase]
    public async Task El_historial_lo_ve_el_dueno_y_el_personal_pero_no_un_ajeno()
    {
        var flow = await SeedAsync();
        await Send(TestRole.Distributor, flow.RequestId + "/start-review");

        var owner = await Send(TestRole.Customer, flow.RequestId + "/history", method: HttpMethod.Get);
        var staff = await Send(TestRole.Engineer, flow.RequestId + "/history", method: HttpMethod.Get);
        var stranger = await Send(TestRole.InstallerCompany, flow.RequestId + "/history", method: HttpMethod.Get);

        Assert.Equal(HttpStatusCode.OK, owner.Status);
        Assert.Equal(HttpStatusCode.OK, staff.Status);
        Assert.Equal(HttpStatusCode.NotFound, stranger.Status);
        Assert.Equal("UnderReview", owner.Body.GetProperty("data").EnumerateArray().Last().GetProperty("newState").GetString());
    }
}
