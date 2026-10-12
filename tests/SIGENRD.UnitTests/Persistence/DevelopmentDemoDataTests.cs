using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Seeds;

namespace SIGENRD.UnitTests.Persistence;

/// <summary>Datos de demostración del mapa (solo Development): deterministas, en el país y con todos los estados.</summary>
public class DevelopmentDemoDataTests
{
    private static readonly Distributor Edesur = new() { Id = 1, Name = DevelopmentDemoData.DistributorName };
    private static readonly IReadOnlyList<int> ZoneIds = Enumerable.Range(10, DevelopmentDemoData.Cities.Count).ToList();
    private static readonly DateTime Now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Siempre_genera_lo_mismo()
    {
        var first = DevelopmentDemoData.Build(500, ZoneIds, Edesur, Now);
        var second = DevelopmentDemoData.Build(500, ZoneIds, Edesur, Now);

        Assert.Equal(
            first.Select(t => (t.Code, t.Location.X, t.Location.Y, t.Status)),
            second.Select(t => (t.Code, t.Location.X, t.Location.Y, t.Status)));
        Assert.Equal("TR-DEMO-00001", first[0].Code);
    }

    [Fact]
    public void Diez_mil_dentro_de_republica_dominicana_con_todos_los_estados()
    {
        var all = DevelopmentDemoData.Build(10_000, ZoneIds, Edesur, Now);

        Assert.Equal(10_000, all.Count);
        Assert.Equal(10_000, all.Select(t => t.Code).Distinct().Count());
        Assert.All(all, t =>
        {
            Assert.InRange(t.Location.Y, 17.4, 20.0);
            Assert.InRange(t.Location.X, -72.1, -68.3);
            Assert.Contains(t.ServiceZoneId, ZoneIds);
        });

        var byStatus = all.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
        foreach (var status in Enum.GetValues<TransformerStatus>())
            Assert.True(byStatus.GetValueOrDefault(status) > 100, $"{status}: {byStatus.GetValueOrDefault(status)}");

        // Algunos con dato viejo (más de 30 días), para ver el aviso.
        Assert.Contains(all, t => (Now - t.LastUpdated).TotalDays > 30);
    }
}
