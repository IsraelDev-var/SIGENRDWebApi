using Microsoft.EntityFrameworkCore;
using Npgsql;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Las pruebas de integración corren solo si existe la variable de entorno ENERGYMAP_TEST_DB con la
/// conexión a un servidor PostgreSQL con PostGIS, por ejemplo:
/// Host=localhost;Port=5433;Username=postgres;Password=...
/// Cada ejecución crea su propia base, aplica las migraciones y la borra al terminar.
/// </summary>
public static class TestDatabase
{
    public const string EnvironmentVariable = "ENERGYMAP_TEST_DB";

    public static string? ServerConnection => Environment.GetEnvironmentVariable(EnvironmentVariable);
}

public sealed class FactIfDatabaseAttribute : FactAttribute
{
    public FactIfDatabaseAttribute()
    {
        if (string.IsNullOrWhiteSpace(TestDatabase.ServerConnection))
            Skip = "Define " + TestDatabase.EnvironmentVariable + " para ejecutar las pruebas contra PostGIS.";
    }
}

public class FakeCurrentUser(string? userId) : ICurrentUserService
{
    public string? UserId => userId;
}

public class PostgisFixture : IAsyncLifetime
{
    public const string UserId = "user-integration-test";

    private string _connectionString = string.Empty;

    public int DistributorId { get; private set; }
    public int ZoneSantoDomingoId { get; private set; }
    public int ZoneSantiagoId { get; private set; }

    public AppContextSIGENRD NewContext(string? userId = UserId)
    {
        var options = new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new AppContextSIGENRD(options, new FakeCurrentUser(userId));
    }

    public async Task InitializeAsync()
    {
        var server = TestDatabase.ServerConnection;
        if (string.IsNullOrWhiteSpace(server)) return;

        var builder = new NpgsqlConnectionStringBuilder(server)
        {
            Database = "it_" + Guid.NewGuid().ToString("N")[..12]
        };
        _connectionString = builder.ConnectionString;

        await using var context = NewContext();
        await context.Database.MigrateAsync();

        var distributor = new Distributor { Name = "Edesur", AvailableThresholdPct = 70, ConditionedThresholdPct = 30 };
        var zoneSd = new ServiceZone { ZoneName = "Santo Domingo Este" };
        var zoneSt = new ServiceZone { ZoneName = "Santiago" };
        context.AddRange(distributor, zoneSd, zoneSt);
        await context.SaveChangesAsync();

        DistributorId = distributor.Id;
        ZoneSantoDomingoId = zoneSd.Id;
        ZoneSantiagoId = zoneSt.Id;

        // Cuatro transformadores en Santo Domingo, uno de cada estado, y uno en Santiago (~140 km).
        await AddTransformer(context, "TR-SD1", zoneSd.Id, 18.4861, -69.9312, 1000, 900, distributor);
        await AddTransformer(context, "TR-SD2", zoneSd.Id, 18.4900, -69.9300, 1000, 400, distributor);
        await AddTransformer(context, "TR-SD3", zoneSd.Id, 18.4950, -69.9200, 1000, 100, distributor);
        await AddTransformer(context, "TR-SD4", zoneSd.Id, 18.5000, -69.9000, 1000, 0, distributor);
        await AddTransformer(context, "TR-ST1", zoneSt.Id, 19.4517, -70.6970, 1000, 800, distributor);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var context = NewContext();
        await context.Database.EnsureDeletedAsync();
        NpgsqlConnection.ClearAllPools();
    }

    public async Task<Transformer> AddTransformer(
        AppContextSIGENRD context, string code, int zoneId, double lat, double lng,
        decimal total, decimal available, Distributor? distributor = null, decimal reserved = 0)
    {
        var transformer = new Transformer
        {
            Code = code,
            DistributorId = DistributorId == 0 ? distributor!.Id : DistributorId,
            ServiceZoneId = zoneId,
            Location = TransformerQueries.ToPoint(lat, lng),
            ReservedCapacityKva = reserved
        };
        transformer.UpdateCapacity(total, available, 70, 30);

        await context.Transformers.AddAsync(transformer);
        return transformer;
    }
}
