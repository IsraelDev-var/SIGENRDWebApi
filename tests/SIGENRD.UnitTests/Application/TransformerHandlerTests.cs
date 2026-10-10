using System.Text;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer;
using SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers;
using SIGENRD.Core.Application.Features.Transformers.Commands.SetTransformerMaintenance;
using SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformerById;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.UnitTests.Application;

public class TransformerHandlerTests
{
    private static Point At(double lat, double lng) => new(lng, lat) { SRID = 4326 };

    private static Transformer NewTransformer(int id, string code, decimal total = 1000, decimal available = 800) => new()
    {
        Id = id,
        Code = code,
        DistributorId = 1,
        ServiceZoneId = 1,
        TotalCapacityKva = total,
        AvailableCapacityKva = available,
        Location = At(18.4861, -69.9312),
        Distributor = new Distributor { Id = 1, Name = "Edesur", AvailableThresholdPct = 70, ConditionedThresholdPct = 30 }
    };

    private static (FakeUnitOfWork uow, FakeTransformerRepository repo) Setup()
    {
        var uow = new FakeUnitOfWork();
        uow.Fake<Distributor>().Items.Add(new Distributor { Id = 1, Name = "Edesur", AvailableThresholdPct = 70, ConditionedThresholdPct = 30 });
        uow.Fake<ServiceZone>().Items.Add(new ServiceZone { Id = 1, ZoneName = "Santo Domingo Este" });
        return (uow, new FakeTransformerRepository());
    }

    // ---------------- mapa ----------------

    [Fact]
    public async Task Mapa_pide_un_registro_mas_para_saber_si_hay_recorte()
    {
        var (_, repo) = Setup();
        for (var i = 1; i <= 5; i++) repo.Items.Add(NewTransformer(i, "TR-" + i));

        var query = new GetTransformersInBoundsQuery { MinLatitude = 18, MaxLatitude = 19, MinLongitude = -70, MaxLongitude = -69, Limit = 3 };
        var result = await new GetTransformersInBoundsHandler(repo).Handle(query, default);

        Assert.Equal(4, repo.LastLimit);
        Assert.True(result.Data!.Truncated);
        Assert.Equal(3, result.Data.Items.Count);
    }

    [Fact]
    public async Task Mapa_sin_recorte_cuando_cabe_en_el_limite()
    {
        var (_, repo) = Setup();
        repo.Items.Add(NewTransformer(1, "TR-1"));

        var query = new GetTransformersInBoundsQuery { MinLatitude = 18, MaxLatitude = 19, MinLongitude = -70, MaxLongitude = -69, Limit = 10 };
        var result = await new GetTransformersInBoundsHandler(repo).Handle(query, default);

        Assert.False(result.Data!.Truncated);
        var point = Assert.Single(result.Data.Items);
        Assert.Equal(18.4861, point.Latitude);
        Assert.Equal(-69.9312, point.Longitude);
        Assert.Equal(80m, point.AvailabilityPct);
    }

    [Fact]
    public async Task Los_filtros_llegan_al_repositorio()
    {
        var (_, repo) = Setup();
        var query = new GetTransformersInBoundsQuery
        {
            MinLatitude = 18, MaxLatitude = 19, MinLongitude = -70, MaxLongitude = -69,
            DistributorId = 1, ServiceZoneId = 7, Status = TransformerStatus.Critical, MinAvailabilityPct = 10
        };

        await new GetTransformersInBoundsHandler(repo).Handle(query, default);

        Assert.Equal(1, repo.LastFilter!.DistributorId);
        Assert.Equal(7, repo.LastFilter.ServiceZoneId);
        Assert.Equal(TransformerStatus.Critical, repo.LastFilter.Status);
        Assert.Equal(10m, repo.LastFilter.MinAvailabilityPct);
    }

    [Fact]
    public async Task Detalle_de_un_transformador_inexistente_es_404()
    {
        var (_, repo) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new GetTransformerByIdHandler(repo).Handle(new GetTransformerByIdQuery(99), default));
    }

    // ---------------- crear ----------------

    [Fact]
    public async Task Crear_calcula_el_estado_con_los_umbrales_de_la_distribuidora()
    {
        var (uow, repo) = Setup();
        var command = new CreateTransformerCommand
        {
            Code = " TR-100 ", DistributorId = 1, ServiceZoneId = 1,
            TotalCapacityKva = 1000, AvailableCapacityKva = 200, Latitude = 18.5, Longitude = -69.9
        };

        var result = await new CreateTransformerHandler(uow, repo).Handle(command, default);

        Assert.True(result.Succeeded);
        var created = Assert.Single(repo.Items);
        Assert.Equal("TR-100", created.Code);
        Assert.Equal(TransformerStatus.Critical, created.Status);
        Assert.Equal(4326, created.Location.SRID);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task Crear_con_codigo_repetido_falla_sin_guardar()
    {
        var (uow, repo) = Setup();
        repo.Items.Add(NewTransformer(1, "TR-100"));
        var command = new CreateTransformerCommand
        {
            Code = "TR-100", DistributorId = 1, ServiceZoneId = 1,
            TotalCapacityKva = 1000, AvailableCapacityKva = 200, Latitude = 18.5, Longitude = -69.9
        };

        await Assert.ThrowsAsync<ApiException>(() => new CreateTransformerHandler(uow, repo).Handle(command, default));
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task Crear_con_zona_inexistente_falla()
    {
        var (uow, repo) = Setup();
        var command = new CreateTransformerCommand
        {
            Code = "TR-1", DistributorId = 1, ServiceZoneId = 99,
            TotalCapacityKva = 1000, AvailableCapacityKva = 200, Latitude = 18.5, Longitude = -69.9
        };

        await Assert.ThrowsAsync<ApiException>(() => new CreateTransformerHandler(uow, repo).Handle(command, default));
    }

    // ---------------- capacidad y mantenimiento ----------------

    [Fact]
    public async Task Actualizar_capacidad_reclasifica_y_guarda()
    {
        var (uow, repo) = Setup();
        repo.Items.Add(NewTransformer(1, "TR-1", total: 1000, available: 900));

        var result = await new UpdateTransformerCapacityHandler(uow, repo)
            .Handle(new UpdateTransformerCapacityCommand { Id = 1, TotalCapacityKva = 1000, AvailableCapacityKva = 0 }, default);

        Assert.Equal("Saturated", result.Data!.Status);
        Assert.False(result.Data.AcceptsNewRequests);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task Actualizar_capacidad_de_un_transformador_inexistente_es_404()
    {
        var (uow, repo) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new UpdateTransformerCapacityHandler(uow, repo)
                .Handle(new UpdateTransformerCapacityCommand { Id = 5, TotalCapacityKva = 10, AvailableCapacityKva = 1 }, default));
    }

    [Fact]
    public async Task Mantenimiento_entra_y_sale()
    {
        var (uow, repo) = Setup();
        repo.Items.Add(NewTransformer(1, "TR-1", total: 1000, available: 900));
        var handler = new SetTransformerMaintenanceHandler(uow, repo);

        var inside = await handler.Handle(new SetTransformerMaintenanceCommand { Id = 1, InMaintenance = true }, default);
        var outside = await handler.Handle(new SetTransformerMaintenanceCommand { Id = 1, InMaintenance = false }, default);

        Assert.Equal("Maintenance", inside.Data!.Status);
        Assert.Equal("Available", outside.Data!.Status);
    }

    // ---------------- importación ----------------

    private const string Header = "code,serviceZoneId,totalCapacityKva,availableCapacityKva,latitude,longitude";

    private static ImportTransformersCommand Import(params string[] lines) => new()
    {
        DistributorId = 1,
        Content = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines)))
    };

    [Fact]
    public async Task Importacion_valida_crea_todo_con_su_estado()
    {
        var (uow, repo) = Setup();

        var result = await new ImportTransformersHandler(uow, repo).Handle(
            Import(Header, "TR-1,1,1000,900,18.4,-69.9", "TR-2,1,1000,100,18.5,-69.8", "TR-3,1,1000,0,18.6,-69.7"), default);

        Assert.Equal(3, result.Data!.Imported);
        Assert.Equal(
            new[] { TransformerStatus.Available, TransformerStatus.Critical, TransformerStatus.Saturated },
            repo.Items.Select(t => t.Status).ToArray());
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task Importacion_es_todo_o_nada_si_una_fila_falla()
    {
        var (uow, repo) = Setup();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => new ImportTransformersHandler(uow, repo).Handle(
            Import(Header, "TR-1,1,1000,900,18.4,-69.9", "TR-2,1,1000,5000,18.5,-69.8"), default));

        Assert.Contains(ex.Errors, e => e.StartsWith("Línea 3:"));
        Assert.Empty(repo.Items);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task Importacion_rechaza_codigos_que_ya_existen_y_zonas_inexistentes()
    {
        var (uow, repo) = Setup();
        repo.Items.Add(NewTransformer(1, "TR-1"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => new ImportTransformersHandler(uow, repo).Handle(
            Import(Header, "TR-1,1,1000,900,18.4,-69.9", "TR-2,42,1000,900,18.5,-69.8"), default));

        Assert.Contains(ex.Errors, e => e.Contains("ya existe") && e.Contains("TR-1"));
        Assert.Contains(ex.Errors, e => e.Contains("zona 42"));
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Importacion_a_una_distribuidora_inexistente_falla()
    {
        var (uow, repo) = Setup();
        var command = Import(Header, "TR-1,1,1000,900,18.4,-69.9");
        command.DistributorId = 77;

        await Assert.ThrowsAsync<ApiException>(() => new ImportTransformersHandler(uow, repo).Handle(command, default));
    }
}
