using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.UnitTests.Domain;

/// <summary>RN-01, RN-02 y RN-03.</summary>
public class TransformerAvailabilityTests
{
    [Theory]
    [InlineData(1000, 700, 0, 70)]      // caso exitoso
    [InlineData(1000, 1000, 0, 100)]    // límite superior
    [InlineData(1000, 0, 0, 0)]         // límite inferior
    [InlineData(1000, 700, 100, 60)]    // las reservas se descuentan
    [InlineData(1000, 100, 300, -20)]   // sobrecomprometido: negativo
    [InlineData(3, 1, 0, 33.33)]        // redondeo a dos decimales
    [InlineData(0, 100, 0, 0)]          // sin capacidad total no hay división por cero
    public void Porcentaje_de_disponibilidad(decimal total, decimal available, decimal reserved, decimal expected)
    {
        Assert.Equal(expected, TransformerAvailability.Percentage(total, available, reserved));
    }

    [Theory]
    [InlineData(100, TransformerStatus.Available)]
    [InlineData(70, TransformerStatus.Available)]
    [InlineData(69.99, TransformerStatus.Conditioned)]
    [InlineData(30, TransformerStatus.Conditioned)]
    [InlineData(29.99, TransformerStatus.Critical)]
    [InlineData(0.01, TransformerStatus.Critical)]
    [InlineData(0, TransformerStatus.Saturated)]
    [InlineData(-5, TransformerStatus.Saturated)]
    public void Estado_segun_umbrales_por_defecto_sin_huecos(decimal pct, TransformerStatus expected)
    {
        Assert.Equal(expected, TransformerAvailability.Classify(pct));
    }

    [Fact]
    public void Los_umbrales_son_configurables_por_distribuidora()
    {
        // Con umbrales 80/50, un 75% ya no es Disponible.
        Assert.Equal(TransformerStatus.Conditioned, TransformerAvailability.Classify(75, 80, 50));
        Assert.Equal(TransformerStatus.Critical, TransformerAvailability.Classify(45, 80, 50));
    }

    [Theory]
    [InlineData(TransformerStatus.Available, true)]
    [InlineData(TransformerStatus.Conditioned, true)]
    [InlineData(TransformerStatus.Critical, true)]
    [InlineData(TransformerStatus.Saturated, false)]
    [InlineData(TransformerStatus.Maintenance, false)]
    public void Solo_algunos_estados_aceptan_solicitudes_nuevas(TransformerStatus status, bool expected)
    {
        Assert.Equal(expected, TransformerAvailability.AcceptsNewRequests(status));
    }

    [Fact]
    public void Actualizar_capacidad_recalcula_el_estado_y_la_fecha()
    {
        var transformer = new Transformer { LastUpdated = DateTime.UtcNow.AddDays(-30) };

        transformer.UpdateCapacity(1000, 250, 70, 30);

        Assert.Equal(25m, transformer.AvailabilityPct);
        Assert.Equal(TransformerStatus.Critical, transformer.Status);
        Assert.True(transformer.LastUpdated > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Mantenimiento_no_se_pisa_al_recalcular()
    {
        var transformer = new Transformer { TotalCapacityKva = 1000, AvailableCapacityKva = 900 };
        transformer.EnterMaintenance();

        transformer.RecalculateStatus(70, 30);
        transformer.UpdateCapacity(1000, 900, 70, 30);

        Assert.Equal(TransformerStatus.Maintenance, transformer.Status);
        Assert.False(transformer.AcceptsNewRequests);
    }

    [Fact]
    public void Salir_de_mantenimiento_reclasifica_con_la_capacidad_actual()
    {
        var transformer = new Transformer { TotalCapacityKva = 1000, AvailableCapacityKva = 0 };
        transformer.EnterMaintenance();

        transformer.LeaveMaintenance(70, 30);

        Assert.Equal(TransformerStatus.Saturated, transformer.Status);
    }
}
