namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una empresa instaladora certificada por la SIE.
    /// </summary>
    public class InstallerCompany
    {
        public int Id { get; set; }

        // Nombre comercial de la empresa
        public string TradeName { get; set; } = string.Empty;

        // RNC de la empresa
        public string Rnc { get; set; } = string.Empty;

        // Correo de contacto
        public string? ContactEmail { get; set; }

        // Teléfono principal
        public string? ContactPhone { get; set; }

        // Dirección física
        public string? Address { get; set; }

        //// Si la empresa está activa
        //public bool IsActive { get; set; } = true;



        // Relaciones
        public ICollection<EngineerUser>? Users { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }

}
