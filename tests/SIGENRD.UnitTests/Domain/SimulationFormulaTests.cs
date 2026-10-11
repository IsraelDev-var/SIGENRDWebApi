using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules.Simulation;

namespace SIGENRD.UnitTests.Domain;

/// <summary>Fórmula de la simulación de medición neta (RF-19 a RF-21, CA-09) y recomendaciones (RF-25).</summary>
public class SimulationFormulaTests
{
    private static readonly SimulationAssumptions Defaults = new();
    private static readonly NetMeteringFormulaV1 Formula = new();

    // ---------------- tarifa BTS-1 ----------------

    /// <summary>
    /// Totales publicados para Edesur (jul-sep 2026, con cargo fijo de RD$128.59): 700 kWh = RD$7,353.59 y
    /// 701 kWh = RD$9,304.68. Sin el cargo fijo, el cargo por energía debe dar exactamente la diferencia.
    /// </summary>
    [Theory]
    [InlineData(700, 7353.59 - 128.59)]
    [InlineData(701, 9304.68 - 128.59)]
    [InlineData(150, 150 * 6.05)]
    [InlineData(250, 200 * 6.05 + 50 * 8.59)]
    [InlineData(400, 200 * 6.05 + 100 * 8.59 + 100 * 12.89)]
    [InlineData(0, 0)]
    public void El_cargo_por_energia_coincide_con_la_tarifa_publicada(double kwh, double expected)
    {
        Assert.Equal(Math.Round((decimal)expected, 2), Math.Round(SimulationAssumptions.DefaultTariff.EnergyCharge((decimal)kwh), 2));
    }

    // ---------------- fórmula ----------------

    [Fact]
    public void Caso_calculado_a_mano_sin_excedente()
    {
        // 400 kWh/mes, 2 kWp × 1500 kWh/kWp = 3000 kWh/año = 250 kWh/mes.
        // Sin paneles: 3358/mes. Con paneles se consumen 150 kWh de la red: 907.50/mes.
        var result = Formula.Calculate(new SimulationInput(400m, 2m), Defaults);

        Assert.Equal(3000m, result.AnnualProductionKwh);
        Assert.Equal(4800m, result.AnnualConsumptionKwh);
        Assert.Equal(62.5m, result.CoveragePct);
        Assert.Equal(40296m, result.AnnualBillBefore);
        Assert.Equal(10890m, result.AnnualBillAfter);
        Assert.Equal(0m, result.AnnualExportCreditKwh);
        Assert.Equal(29406m, result.AnnualSavings);
        Assert.Equal(120000m, result.InvestmentCost);       // 2 kWp × RD$60,000
        Assert.Equal(4.08m, result.PaybackYears);            // 120000 / 29406
        Assert.Equal(ViabilityStatus.Profitable, result.Viability);

        // ROI: ahorro de 25 años con 0,5 % de degradación anual (suma geométrica), menos la inversión.
        var lifetime = 29406.0 * (1 - Math.Pow(0.995, 25)) / 0.005;
        var expectedRoi = (lifetime - 120000) / 120000 * 100;
        Assert.Equal(Math.Round(expectedRoi, 2), (double)result.Roi, 2);
    }

    [Fact]
    public void El_excedente_se_acumula_y_se_paga_al_75_por_ciento_del_primer_tramo()
    {
        // 100 kWh/mes de consumo y 250 kWh/mes de producción: sobran 150 kWh cada mes.
        var result = Formula.Calculate(new SimulationInput(100m, 2m), Defaults);

        Assert.Equal(0m, result.AnnualBillAfter);
        Assert.Equal(1800m, result.AnnualExportCreditKwh);
        Assert.Equal(8167.5m, result.SurplusPayment);        // 1800 × 6.05 × 0.75
        Assert.Equal(7260m + 8167.5m, result.AnnualSavings); // 100 × 6.05 × 12 + pago
        Assert.Equal(250m, result.CoveragePct);
    }

    [Fact]
    public void Desde_701_kWh_todo_el_consumo_se_cobra_al_ultimo_tramo()
    {
        var result = Formula.Calculate(new SimulationInput(701m, 0.1m), Defaults);

        Assert.Equal(Math.Round(701m * 13.09m * 12m, 2), result.AnnualBillBefore);
    }

    [Fact]
    public void Con_precio_fijo_no_se_usa_la_tarifa_escalonada()
    {
        var result = Formula.Calculate(new SimulationInput(1000m, 4m, FlatPricePerKwh: 15m), Defaults);

        Assert.Equal(1000m * 15m * 12m, result.AnnualBillBefore);
        Assert.Equal(Math.Round((1000m - 500m) * 15m * 12m, 2), result.AnnualBillAfter);
    }

    [Theory]
    [InlineData(100000, ViabilityStatus.Profitable)]              // 3.40 años
    [InlineData(250000, ViabilityStatus.ConditionallyProfitable)] // 8.50 años
    [InlineData(400000, ViabilityStatus.NotProfitable)]           // 13.60 años
    public void Clasifica_segun_los_anos_de_retorno(double investment, ViabilityStatus expected)
    {
        var result = Formula.Calculate(new SimulationInput(400m, 2m, (decimal)investment), Defaults);

        Assert.Equal(expected, result.Viability);
    }

    [Fact]
    public void La_inversion_indicada_reemplaza_la_estimada()
    {
        var result = Formula.Calculate(new SimulationInput(400m, 2m, InvestmentCost: 95000m), Defaults);

        Assert.Equal(95000m, result.InvestmentCost);
        Assert.Equal(Math.Round(95000m / 29406m, 2), result.PaybackYears);
    }

    [Fact]
    public void Los_supuestos_cambian_el_resultado()
    {
        var conservative = Defaults with { SpecificYieldKwhPerKwp = 1300m, CostPerKwp = 70000m };

        var baseline = Formula.Calculate(new SimulationInput(400m, 2m), Defaults);
        var result = Formula.Calculate(new SimulationInput(400m, 2m), conservative);

        Assert.Equal(2600m, result.AnnualProductionKwh);
        Assert.True(result.PaybackYears > baseline.PaybackYears);
    }

    // ---------------- recomendaciones ----------------

    private static IReadOnlyList<string> Recommend(SimulationInput input, NearbyTransformer? transformer = null, EquipmentSummary? equipment = null)
    {
        var result = Formula.Calculate(input, Defaults);
        return SimulationRecommendations.For(input, result, Defaults, transformer, equipment, 0.9m);
    }

    private static NearbyTransformer Available(string code = "TR-1") => new(code, 120, TransformerStatus.Available, 200m);

    [Fact]
    public void Un_sistema_sobredimensionado_sugiere_menos_potencia()
    {
        var list = Recommend(new SimulationInput(100m, 2m), Available());

        Assert.Contains(list, r => r.Contains("cubrirías tu consumo") && r.Contains("0.8 kWp"));
    }

    [Fact]
    public void Un_sistema_pequeno_y_rentable_sugiere_ampliar()
    {
        var list = Recommend(new SimulationInput(1000m, 2m), Available());

        Assert.Contains(list, r => r.Contains("ampliarlo"));
    }

    [Fact]
    public void Si_queda_apenas_por_encima_de_701_kWh_sugiere_bajar_de_tramo()
    {
        // 900 kWh/mes con 1.5 kWp: 187.5 kWh/mes de producción, quedan 712.5 kWh de la red.
        var list = Recommend(new SimulationInput(900m, 1.5m), Available());

        Assert.Contains(list, r => r.Contains("701 kWh") && r.Contains("kWp"));
    }

    [Theory]
    [InlineData(TransformerStatus.Saturated, "no admite nuevas conexiones")]
    [InlineData(TransformerStatus.Maintenance, "no admite nuevas conexiones")]
    [InlineData(TransformerStatus.Critical, "poca capacidad libre")]
    public void Advierte_cuando_el_transformador_cercano_esta_limitado(TransformerStatus status, string expected)
    {
        var list = Recommend(new SimulationInput(400m, 2m), new NearbyTransformer("TR-LIM", 300, status, 50m));

        Assert.Contains(list, r => r.Contains("TR-LIM") && r.Contains(expected));
    }

    [Fact]
    public void Advierte_cuando_el_transformador_no_tiene_los_kVA_necesarios()
    {
        // 5 kWp / 0.9 = 5.6 kVA y el transformador tiene 3 kVA libres.
        var list = Recommend(new SimulationInput(700m, 5m), new NearbyTransformer("TR-POCO", 300, TransformerStatus.Available, 3m));

        Assert.Contains(list, r => r.Contains("TR-POCO") && r.Contains("kVA"));
    }

    [Fact]
    public void Sin_transformador_cercano_lo_indica()
    {
        Assert.Contains(Recommend(new SimulationInput(400m, 2m)), r => r.Contains("No hay transformadores"));
    }

    [Theory]
    [InlineData(6, 3, "recortará producción")]
    [InlineData(3, 5, "inversor menor")]
    public void Revisa_la_relacion_entre_paneles_e_inversor(double panelsKw, double invertersKw, string expected)
    {
        var list = Recommend(new SimulationInput(700m, (decimal)panelsKw), Available(), new EquipmentSummary((decimal)panelsKw, (decimal)invertersKw));

        Assert.Contains(list, r => r.Contains(expected));
    }

    // ---------------- geografía ----------------

    [Fact]
    public void Distancia_entre_Santo_Domingo_y_Santiago()
    {
        var meters = Geo.DistanceMeters(18.4861, -69.9312, 19.4517, -70.6970);

        Assert.InRange(meters, 130_000, 140_000);
    }
}
