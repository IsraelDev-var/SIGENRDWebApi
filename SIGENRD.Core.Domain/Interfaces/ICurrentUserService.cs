namespace SIGENRD.Core.Domain.Interfaces
{
    /// <summary>
    /// Usuario autenticado de la petición actual. Lo usa la auditoría para saber quién hizo cada cambio.
    /// </summary>
    public interface ICurrentUserService
    {
        string? UserId { get; }
    }
}
