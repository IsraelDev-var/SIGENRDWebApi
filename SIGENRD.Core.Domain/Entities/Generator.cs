

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un generador o inversor dentro de una solicitud de medición neta.
    /// </summary>
    public class Generator : AuditableEntity
    {
        public int Id { get; set; }

        // 🔹 Relación con la solicitud de medición neta
        public int NetMeteringRequestId { get; set; }
        public NetMeteringRequest? NetMeteringRequest { get; set; }

        // 🔹 Tipo de tecnología (solar, eólica, etc.)
        public GenerationSystemType GenerationSystemType { get; set; }

        // 🔹 Modelo del inversor
        public string? InverterModel { get; set; }

        // 🔹 Fabricante del equipo
        public string? Manufacturer { get; set; }

        // 🔹 Potencia nominal (kVA)
        public decimal RatedPowerKva { get; set; }

        // 🔹 Voltaje de interconexión
        public decimal ConnectionVoltage { get; set; }

        // 🔹 Corriente nominal
        public decimal NominalCurrent { get; set; }

        // 🔹 Factor de potencia mínimo y máximo
        public decimal PowerFactorMin { get; set; }
        public decimal PowerFactorMax { get; set; }

        // Tipo de generador y datos del equipo (Anexo A, parte I)
        public GeneratorKind GeneratorKind { get; set; } = GeneratorKind.InverterBased;

        // 1 monofásico, 3 trifásico
        public int Phases { get; set; } = 1;

        public string? SerialNumber { get; set; }

        // Estándar y entidad que certifican el equipo (parte G; IEEE 1547 o UL 1741)
        public string? CertificationStandard { get; set; }
        public string? CertifyingUnit { get; set; }

        // 🔹 Tipo de conmutación
        public string? SwitchingType { get; set; }

        // 🔹 Contribución armónica total (THD)
        public decimal HarmonicDistortionPercent { get; set; }


        
    }
}
