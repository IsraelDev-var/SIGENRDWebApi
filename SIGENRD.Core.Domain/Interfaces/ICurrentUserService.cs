namespace SIGENRD.Core.Domain.Interfaces
{
    /// <summary>
    /// Usuario autenticado de la petición actual. Lo usan la auditoría y el control de acceso a los datos.
    /// </summary>
    public interface ICurrentUserService
    {
        string? UserId { get; }

        bool IsInRole(string role);
    }
}
