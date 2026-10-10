using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Ejecutan las consultas y las reglas de integridad contra un PostgreSQL real con PostGIS.
/// Ver PostgisFixture para saber cómo activarlas.
/// </summary>
public class PostgisIntegrationTests(PostgisFixture db) : IClassFixture<PostgisFixture>
{
    private static readonly TransformerFilter NoFilter = new();

    private static string[] Codes(IEnumerable<Transformer> items) => items.Select(t => t.Code).ToArray();

    private static string Flatten(Exception ex)
    {
        var messages = new List<string>();
        for (Exception? e = ex; e is not null; e = e.InnerException) messages.Add(e.Message);
        return string.Join(" | ", messages);
    }

    // ---------------- consultas espaciales ----------------

    [FactIfDatabase]
    public async Task Las_migraciones_crean_PostGIS_y_las_tablas_del_modelo()
    {
        await using var context = db.NewContext();

        var version = await context.Database.SqlQueryRaw<string>("select postgis_version() as \"Value\"").SingleAsync();
        var audit = await context.AuditLogs.CountAsync();

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.True(audit >= 0);
    }

    [FactIfDatabase]
    public async Task Area_visible_devuelve_solo_lo_que_cae_dentro()
    {
        await using var context = db.NewContext();
        var repo = new TransformerRepository(context);

        var santoDomingo = await repo.GetInBoundsAsync(18.40, -70.00, 18.60, -69.80, NoFilter, 100);
        var santiago = await repo.GetInBoundsAsync(19.40, -70.80, 19.50, -70.60, NoFilter, 100);

        Assert.Equal(new[] { "TR-SD1", "TR-SD2", "TR-SD3", "TR-SD4" }, Codes(santoDomingo));
        Assert.Equal(new[] { "TR-ST1" }, Codes(santiago));
    }

    [FactIfDatabase]
    public async Task Area_visible_respeta_el_limite()
    {
        await using var context = db.NewContext();

        var result = await new TransformerRepository(context).GetInBoundsAsync(18.40, -70.00, 18.60, -69.80, NoFilter, 2);

        Assert.Equal(2, result.Count);
    }

    [FactIfDatabase]
    public async Task Cercania_ordena_por_distancia_y_respeta_el_radio()
    {
        await using var context = db.NewContext();
        var repo = new TransformerRepository(context);

        // Desde TR-SD1: TR-SD2 está a ~450 m, TR-SD3 a ~1,5 km, TR-SD4 a ~3,6 km y Santiago a ~140 km.
        var withinOneKm = await repo.GetNearAsync(18.4861, -69.9312, 1000, NoFilter, 50);
        var withinFiveKm = await repo.GetNearAsync(18.4861, -69.9312, 5000, NoFilter, 50);

        Assert.Equal(new[] { "TR-SD1", "TR-SD2" }, Codes(withinOneKm));
        Assert.Equal(new[] { "TR-SD1", "TR-SD2", "TR-SD3", "TR-SD4" }, Codes(withinFiveKm));
    }

    [FactIfDatabase]
    public async Task Filtro_por_estado()
    {
        await using var context = db.NewContext();

        var critical = await new TransformerRepository(context)
            .GetInBoundsAsync(18.40, -70.00, 18.60, -69.80, new TransformerFilter(Status: TransformerStatus.Critical), 100);

        Assert.Equal(new[] { "TR-SD3" }, Codes(critical));
    }

    [FactIfDatabase]
    public async Task Filtro_por_disponibilidad_minima()
    {
        await using var context = db.NewContext();

        var half = await new TransformerRepository(context)
            .GetInBoundsAsync(18.40, -70.00, 18.60, -69.80, new TransformerFilter(MinAvailabilityPct: 50), 100);

        Assert.Equal(new[] { "TR-SD1" }, Codes(half));
    }

    [FactIfDatabase]
    public async Task La_disponibilidad_minima_descuenta_las_reservas()
    {
        // Punta Cana, fuera de las áreas de las demás pruebas.
        await using var context = db.NewContext();
        var zone = db.ZoneSantoDomingoId;
        await db.AddTransformer(context, "TR-PC1", zone, 18.58, -68.40, 1000, 900, reserved: 500);
        await db.AddTransformer(context, "TR-PC2", zone, 18.59, -68.41, 1000, 900, reserved: 0);
        await context.SaveChangesAsync();

        var result = await new TransformerRepository(context)
            .GetInBoundsAsync(18.50, -68.50, 18.70, -68.30, new TransformerFilter(MinAvailabilityPct: 50), 100);

        // TR-PC1: (900 - 500) / 1000 = 40 %, queda fuera; TR-PC2: 90 %.
        Assert.Equal(new[] { "TR-PC2" }, Codes(result));
    }

    [FactIfDatabase]
    public async Task Busqueda_por_codigo_ignora_mayusculas_y_escapa_comodines()
    {
        await using var context = db.NewContext();
        var repo = new TransformerRepository(context);

        var partial = await repo.SearchByCodeAsync("tr-sd", 50);
        var wildcard = await repo.SearchByCodeAsync("R_S", 50);   // _ no debe comportarse como comodín
        var percent = await repo.SearchByCodeAsync("%", 50);      // % tampoco

        Assert.Equal(4, partial.Count);
        Assert.Empty(wildcard);
        Assert.Empty(percent);
    }

    [FactIfDatabase]
    public async Task Resumen_de_riesgo_por_zona()
    {
        await using var context = db.NewContext();

        var zones = await new TransformerRepository(context).GetZoneRiskAsync();

        var santoDomingo = Assert.Single(zones, z => z.ServiceZoneId == db.ZoneSantoDomingoId);
        Assert.Equal(1, santoDomingo.Critical);
        Assert.Equal(1, santoDomingo.Saturated);
        Assert.DoesNotContain(zones, z => z.ServiceZoneId == db.ZoneSantiagoId);
    }

    [FactIfDatabase]
    public async Task Codigo_unico_a_nivel_de_base_de_datos()
    {
        await using var context = db.NewContext();
        await db.AddTransformer(context, "TR-SD1", db.ZoneSantoDomingoId, 18.0, -69.0, 1000, 500);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Contains("IX_Transformers_Code", Flatten(ex));
    }

    // ---------------- auditoría e integridad ----------------

    [FactIfDatabase]
    public async Task Los_cambios_se_auditan_con_el_usuario_y_los_valores()
    {
        await using var context = db.NewContext();
        var transformer = await db.AddTransformer(context, "TR-AUD1", db.ZoneSantoDomingoId, 18.3, -69.5, 1000, 800);
        await context.SaveChangesAsync();

        transformer.UpdateCapacity(1000, 100, 70, 30);
        await context.SaveChangesAsync();

        var logs = await context.AuditLogs
            .Where(a => a.EntityName == nameof(Transformer) && a.EntityId == transformer.Id.ToString())
            .OrderBy(a => a.Id)
            .ToListAsync();

        Assert.Equal(new[] { "Added", "Modified" }, logs.Select(l => l.Action).ToArray());
        Assert.All(logs, l => Assert.Equal(PostgisFixture.UserId, l.UserId));

        var modified = logs[1];
        Assert.Contains("AvailableCapacityKva", modified.OldValues);
        Assert.Contains("800", modified.OldValues);
        Assert.Contains("100", modified.NewValues);
        Assert.Contains("Critical", modified.NewValues);

        Assert.Equal(PostgisFixture.UserId, transformer.CreatedBy);
        Assert.NotNull(transformer.UpdatedAt);
    }

    [FactIfDatabase]
    public async Task La_auditoria_no_se_puede_modificar_ni_borrar()
    {
        await using var context = db.NewContext();
        context.AuditLogs.Add(new AuditLog { Action = "Added", EntityName = "Prueba", EntityId = "1" });
        await context.SaveChangesAsync();

        var update = await Assert.ThrowsAnyAsync<Exception>(() =>
            context.Database.ExecuteSqlRawAsync("UPDATE \"AuditLogs\" SET \"Action\" = 'Alterado'"));
        var delete = await Assert.ThrowsAnyAsync<Exception>(() =>
            context.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditLogs\""));

        Assert.Contains("solo inserci", Flatten(update));
        Assert.Contains("solo inserci", Flatten(delete));
    }

    [FactIfDatabase]
    public async Task El_historial_de_estados_no_se_puede_modificar()
    {
        await using var context = db.NewContext();

        var customer = new Customer { ApplicationUserId = "u-1", FullName = "Cliente de prueba", NationalIdOrRnc = "00100000001" };
        var transformer = await db.AddTransformer(context, "TR-HIST1", db.ZoneSantoDomingoId, 18.2, -69.4, 1000, 800);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var request = new ConnectionRequest
        {
            RequestCode = "REQ-HIST-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = customer.Id,
            DistributorId = db.DistributorId,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10
        };
        context.ConnectionRequests.Add(request);
        await context.SaveChangesAsync();

        var history = new StateHistory { ConnectionRequestId = request.Id, PreviousState = "Received", NewState = "UnderReview", Comment = "Inicio" };
        context.StateHistories.Add(history);
        await context.SaveChangesAsync();

        history.Comment = "Alterado";
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());

        Assert.Contains("solo inserci", Flatten(ex));
    }

    [FactIfDatabase]
    public async Task Codigo_de_solicitud_unico()
    {
        await using var context = db.NewContext();
        var customer = new Customer { ApplicationUserId = "u-2", FullName = "Otro cliente", NationalIdOrRnc = "00100000002" };
        var transformer = await db.AddTransformer(context, "TR-REQ1", db.ZoneSantoDomingoId, 18.1, -69.3, 1000, 800);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        ConnectionRequest Make() => new()
        {
            RequestCode = "REQ-DUP",
            CustomerId = customer.Id,
            DistributorId = db.DistributorId,
            TransformerId = transformer.Id,
            RequestedCapacityKva = 10
        };

        context.ConnectionRequests.Add(Make());
        await context.SaveChangesAsync();

        context.ConnectionRequests.Add(Make());
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Contains("IX_ConnectionRequests_RequestCode", Flatten(ex));
    }
}
