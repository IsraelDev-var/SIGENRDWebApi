namespace SIGENRD.Core.Domain.Interfaces
{
    /// <summary>
    /// Usuario autenticado de la petición actual. Lo usan la auditoría y el control de acceso a los datos.
    /// </summary>
    public interface ICurrentUserService
    {
        string? UserId { get; }

        bool IsInRole(string role);

        /// <summary>IP de origen de la petición, para la auditoría de autenticación. null fuera de una petición HTTP.</summary>
        string? IpAddress => null;

        /// <summary>Nombre del usuario (claim FullName del token), por ejemplo para firmar el certificado.</summary>
        string? FullName => null;

        /// <summary>Distribuidora del personal (Ingeniero o Distribuidora): solo ve y gestiona lo de su EDE. null para el resto.</summary>
        int? DistributorId => null;
    }
}
