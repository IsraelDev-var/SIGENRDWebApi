using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Simulación técnico-económica de medición neta (RF-18 a RF-26). Es informativa: no es una solicitud
    /// y no reserva capacidad del transformador. Guarda los parámetros, los supuestos y la versión de la fórmula
    /// con que se calculó, para que el historial se pueda leer igual aunque los supuestos cambien después.
    /// </summary>
    public class NetMeteringSimulation : AuditableEntity
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        /// <summary>Nombre que le da el usuario para reconocerla al comparar (RF-22).</summary>
        public string? Name { get; set; }

        // ---- Parámetros (RF-18) ----
        public decimal MonthlyConsumptionKwh { get; set; }
        public Point? Location { get; set; }
        public decimal InstalledCapacityKw { get; set; }

        /// <summary>Precio fijo por kWh indicado por el usuario (otra tarifa que BTS-1); null usa la tarifa BTS-1.</summary>
        public decimal? FlatPricePerKwh { get; set; }

        // Transformador y distribuidora de referencia (RF-23), opcionales
        public int? DistributorId { get; set; }
        public int? TransformerId { get; set; }
        public double? TransformerDistanceMeters { get; set; }

        // ---- Resultados (RF-19 a RF-21) ----
        public decimal AnnualProductionKwh { get; set; }
        public decimal CoveragePct { get; set; }
        public decimal AnnualBillBefore { get; set; }
        public decimal AnnualBillAfter { get; set; }
        public decimal AnnualExportCreditKwh { get; set; }
        public decimal SurplusPayment { get; set; }
        public decimal AnnualSavings { get; set; }
        public decimal InvestmentCost { get; set; }

        /// <summary>null si el sistema no genera ahorro.</summary>
        public decimal? PaybackYears { get; set; }
        public decimal Roi { get; set; }

        public ViabilityStatus ViabilityStatus { get; set; }

        /// <summary>Recomendaciones generadas (RF-25).</summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>Versión de la fórmula (Strategy) y supuestos usados, en JSON.</summary>
        public string FormulaVersion { get; set; } = string.Empty;
        public string Assumptions { get; set; } = "{}";

        public Distributor? Distributor { get; set; }
        public Transformer? Transformer { get; set; }
        public ICollection<SimulationGenerator>? Generators { get; set; }
    }
}
