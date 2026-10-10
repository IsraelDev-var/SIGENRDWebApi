using FluentValidation.TestHelper;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.UnitTests.Application;

/// <summary>Datos del formulario de solicitud (Anexo A del reglamento de interconexión).</summary>
public class CreateRequestValidatorTests
{
    private readonly CreateConnectionRequestValidator _validator = new();

    private static CreateConnectionRequestCommand Connection() => new()
    {
        CustomerId = 1, DistributorId = 1, TransformerId = 1,
        RequestType = RequestType.Connection, RequestedCapacityKva = 5,
        UsageType = UsageType.Residential, TariffType = TariffType.BT, InterconnectionType = InterconnectionType.Parallel,
        ProjectAddress = "Calle 1", Latitude = 18.4, Longitude = -69.9, DeclarationAccepted = true
    };

    private static GeneratorInput Inverter() => new()
    {
        Manufacturer = "Huawei", InverterModel = "SUN2000-5KTL", GenerationSystemType = GenerationSystemType.Photovoltaic,
        Phases = 1, RatedPowerKva = 5, ConnectionVoltage = 240, NominalCurrent = 21,
        PowerFactorMin = 0.9m, PowerFactorMax = 1m, HarmonicDistortionPercent = 3
    };

    private static CreateConnectionRequestCommand NetMetering() => new()
    {
        CustomerId = 1, DistributorId = 1, TransformerId = 1,
        RequestType = RequestType.NetMetering, RequestedCapacityKva = 5,
        UsageType = UsageType.Residential, TariffType = TariffType.BT, InterconnectionType = InterconnectionType.Parallel,
        ProjectAddress = "Calle 1", Latitude = 18.4, Longitude = -69.9, DeclarationAccepted = true,
        EngineerFullName = "Ing. Ana Pérez", EngineerCodiaNumber = "12345",
        ServiceContractNumber = "C-0001", MeterNumber = "M-0001",
        NetMetering = new NetMeteringDetailsInput
        {
            GenerationSystemType = GenerationSystemType.Photovoltaic, InstalledCapacityKw = 5,
            Generators = new List<GeneratorInput> { Inverter() }
        }
    };

    [Fact]
    public void Conexion_pequena_valida() => _validator.TestValidate(Connection()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Medicion_neta_valida() => _validator.TestValidate(NetMetering()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Sin_certificar_la_informacion_no_se_acepta()
    {
        var command = Connection();
        command.DeclarationAccepted = false;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.DeclarationAccepted);
    }

    [Theory]
    [InlineData(PropertyRelation.CoOwner)]
    [InlineData(PropertyRelation.Tenant)]
    [InlineData(PropertyRelation.Other)]
    public void Si_no_es_el_dueno_se_piden_los_datos_del_dueno(PropertyRelation relation)
    {
        var command = Connection();
        command.PropertyRelation = relation;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerFullName);
        result.ShouldHaveValidationErrorFor(c => c.OwnerNationalId);
    }

    [Fact]
    public void Si_es_el_dueno_no_se_piden_sus_datos()
    {
        _validator.TestValidate(Connection()).ShouldNotHaveValidationErrorFor(c => c.OwnerFullName);
    }

    [Fact]
    public void Por_encima_de_10_kVA_se_pide_el_ingeniero()
    {
        var command = Connection();
        command.RequestedCapacityKva = 30;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EngineerFullName);
        result.ShouldHaveValidationErrorFor(c => c.EngineerCodiaNumber);
    }

    [Fact]
    public void En_10_kVA_no_se_pide_el_ingeniero()
    {
        var command = Connection();
        command.RequestedCapacityKva = 10;

        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(c => c.EngineerFullName);
    }

    [Fact]
    public void Medicion_neta_sin_datos_de_generacion_es_invalida()
    {
        var command = NetMetering();
        command.NetMetering = null;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.NetMetering);
    }

    [Fact]
    public void Medicion_neta_sin_generadores_es_invalida()
    {
        var command = NetMetering();
        command.NetMetering!.Generators.Clear();

        _validator.TestValidate(command).ShouldHaveValidationErrorFor("NetMetering.Generators");
    }

    [Theory]
    [InlineData(nameof(CreateConnectionRequestCommand.ServiceContractNumber))]
    [InlineData(nameof(CreateConnectionRequestCommand.MeterNumber))]
    [InlineData(nameof(CreateConnectionRequestCommand.EngineerCodiaNumber))]
    public void Medicion_neta_exige_contrato_medidor_e_ingeniero(string property)
    {
        var command = NetMetering();
        typeof(CreateConnectionRequestCommand).GetProperty(property)!.SetValue(command, null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(property);
    }

    [Fact]
    public void Una_conexion_no_lleva_datos_de_generacion()
    {
        var command = Connection();
        command.NetMetering = NetMetering().NetMetering;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.NetMetering);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    public void La_conexion_del_inversor_es_monofasica_o_trifasica(int phases)
    {
        var command = NetMetering();
        command.NetMetering!.Generators[0].Phases = phases;

        Assert.False(_validator.TestValidate(command).IsValid);
    }

    [Fact]
    public void El_factor_de_potencia_minimo_no_supera_al_maximo()
    {
        var command = NetMetering();
        command.NetMetering!.Generators[0].PowerFactorMin = 1m;
        command.NetMetering.Generators[0].PowerFactorMax = 0.9m;

        Assert.False(_validator.TestValidate(command).IsValid);
    }

    [Fact]
    public void El_fabricante_del_inversor_es_obligatorio()
    {
        var command = NetMetering();
        command.NetMetering!.Generators[0].Manufacturer = "";

        Assert.False(_validator.TestValidate(command).IsValid);
    }
}
