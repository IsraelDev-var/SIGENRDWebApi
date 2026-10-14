namespace SIGENRD.Core.Domain.Interfaces
{
    /// <summary>
    /// Eventos de autenticación que RN-20 exige registrar (inicio de sesión, bloqueo, restablecimiento de contraseña).
    /// Se guardan en AuditLogs junto a los cambios de datos.
    /// </summary>
    public interface ISecurityAuditLog
    {
        /// <param name="action">Uno de <see cref="SecurityEvents"/>.</param>
        /// <param name="userId">Usuario afectado; null si el correo no corresponde a ninguna cuenta.</param>
        /// <param name="email">Correo con el que se intentó la acción.</param>
        Task RecordAsync(string action, string? userId, string email, CancellationToken cancellationToken = default);
    }

    /// <summary>Valores de AuditLog.Action para los eventos de seguridad (EntityName = "Account").</summary>
    public static class SecurityEvents
    {
        public const string EntityName = "Account";

        public const string LoginSucceeded = "LoginSucceeded";
        public const string LoginFailed = "LoginFailed";
        public const string LoginLockedOut = "LoginLockedOut";
        public const string PasswordResetRequested = "PasswordResetRequested";
        public const string PasswordResetCompleted = "PasswordResetCompleted";
        public const string AccountDeactivated = "AccountDeactivated";
        public const string AccountReactivated = "AccountReactivated";
        public const string LoginRejectedDeactivated = "LoginRejectedDeactivated";
    }
}
