namespace SIGENRD.Core.Application.Interfaces.Services
{
    /// <summary>
    /// Personal de la distribuidora y administración, para avisarles de lo que llega (RN-16, RN-17).
    /// La implementación vive en Identity, que es quien conoce los usuarios y sus roles.
    /// </summary>
    public interface IStaffDirectory
    {
        Task<IReadOnlyList<string>> GetUserIdsInRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default);
    }
}
