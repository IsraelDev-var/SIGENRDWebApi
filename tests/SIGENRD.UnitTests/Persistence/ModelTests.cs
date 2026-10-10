using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.UnitTests.Persistence;

/// <summary>
/// Construye el modelo de EF con el proveedor real (Npgsql + NetTopologySuite), sin base de datos.
/// Detecta errores de mapeo antes de generar migraciones.
/// </summary>
public class ModelTests
{
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql("Host=localhost;Database=model-test", npgsql => npgsql.UseNetTopologySuite())
            .Options;

        using var context = new AppContextSIGENRD(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void Transformer_location_se_guarda_como_geography_4326()
    {
        var property = BuildModel().FindEntityType(typeof(Transformer))!.FindProperty(nameof(Transformer.Location))!;

        Assert.Equal("geography (Point, 4326)", property.GetColumnType());
    }

    [Theory]
    [InlineData(typeof(AuditLog), "AuditLogs")]
    [InlineData(typeof(Notification), "Notifications")]
    [InlineData(typeof(UserProfile), "UserProfiles")]
    [InlineData(typeof(NetMeteringSimulation), "NetMeteringSimulations")]
    [InlineData(typeof(SimulationGenerator), "SimulationGenerators")]
    public void Las_tablas_del_modelo_v12_existen(Type entity, string table)
    {
        Assert.Equal(table, BuildModel().FindEntityType(entity)!.GetTableName());
    }

    [Fact]
    public void Los_estados_se_guardan_como_texto()
    {
        var model = BuildModel();

        Assert.Equal(typeof(string), model.FindEntityType(typeof(Transformer))!.FindProperty(nameof(Transformer.Status))!.GetProviderClrType());
        Assert.Equal(typeof(string), model.FindEntityType(typeof(ConnectionRequest))!.FindProperty(nameof(ConnectionRequest.Status))!.GetProviderClrType());
    }

    [Fact]
    public void La_solicitud_puede_no_tener_instaladora()
    {
        var property = BuildModel().FindEntityType(typeof(ConnectionRequest))!.FindProperty(nameof(ConnectionRequest.InstallerCompanyId))!;

        Assert.True(property.IsNullable);
    }
}
