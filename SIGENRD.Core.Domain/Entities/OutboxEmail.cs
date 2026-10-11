namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Correo pendiente de envío (bandeja de salida). La petición solo lo guarda; un proceso en segundo plano
    /// lo envía y reintenta si el servidor SMTP falla, así un SMTP caído no rompe ni frena la petición.
    /// No se audita: el cuerpo puede llevar códigos de confirmación o de restablecimiento.
    /// </summary>
    public class OutboxEmail
    {
        public long Id { get; set; }

        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        /// <summary>Se borra al enviarse: no se guardan enlaces con códigos más tiempo del necesario.</summary>
        public string? Body { get; set; }

        public string? From { get; set; }

        public DateTime CreatedAt { get; set; }

        public int Attempts { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public string? LastError { get; set; }

        public DateTime? SentAt { get; set; }

        /// <summary>Se agotaron los reintentos: queda para revisión manual.</summary>
        public DateTime? FailedAt { get; set; }
    }
}
