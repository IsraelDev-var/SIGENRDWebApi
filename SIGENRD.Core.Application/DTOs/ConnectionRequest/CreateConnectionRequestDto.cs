using SIGENRD.Core.Domain.Enums;

using System.ComponentModel.DataAnnotations;


namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class CreateConnectionRequestDto
    {
        // IDs de las relaciones (Obligatorios)
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "La compañía instaladora es obligatoria.")]
        public int InstallerCompanyId { get; set; }

        [Required(ErrorMessage = "La distribuidora es obligatoria.")]
        public int DistributorId { get; set; }

        // El transformador es opcional al inicio
        public int? TransformerId { get; set; }

        // Enums (Selectores en el Frontend)
        [Required]
        public UsageType UsageType { get; set; } // Residencial, Comercial...

        [Required]
        public TariffType TariffType { get; set; } // BTS1, BTS2...

        [Required]
        public InterconnectionType InterconnectionType { get; set; } // Isla, Paralelo...

        // Datos descriptivos
        public string? ProjectDescription { get; set; }

        [Required(ErrorMessage = "La dirección del proyecto es obligatoria.")]
        public string ProjectAddress { get; set; } = string.Empty;

        // 🌍 Coordenadas: El Frontend envía Lat/Lon simples (doubles)
        // Nosotros las convertiremos a 'Point' en el Mapper
        [Required]
        [Range(-90, 90, ErrorMessage = "Latitud inválida.")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitud inválida.")]
        public double Longitude { get; set; }
    }
}
