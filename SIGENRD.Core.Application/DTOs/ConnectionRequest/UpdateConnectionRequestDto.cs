using SIGENRD.Core.Domain.Enums;


using System.ComponentModel.DataAnnotations;


namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class UpdateConnectionRequestDto
    {
        [Required]
        public int Id { get; set; } // Necesario para identificar qué actualizamos

        // Permitimos corregir la distribuidora o transformador
        public int DistributorId { get; set; }
        public int? TransformerId { get; set; }

        // Permitimos corregir datos técnicos
        public UsageType UsageType { get; set; }
        public TariffType TariffType { get; set; }
        public InterconnectionType InterconnectionType { get; set; }

        // Permitimos corregir descripción y ubicación
        public string? ProjectDescription { get; set; }
        public string? ProjectAddress { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
