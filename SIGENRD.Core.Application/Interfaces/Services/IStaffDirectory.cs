namespace SIGENRD.Core.Application.Interfaces.Services
{
    /// <summary>
    /// Personal de la distribuidora y administración, para avisarles de lo que llega (RN-16, RN-17).
    /// La implementación vive en Identity, que es quien conoce los usuarios y sus roles.
    /// </summary>
    public interface IStaffDirectory
    {
        /// <summary>
        /// Usuarios con alguno de los roles. Con distributorId, los de rol Admin siempre entran y el resto solo si
        /// es personal de esa EDE.
        /// </summary>
        Task<IReadOnlyList<string>> GetUserIdsInRolesAsync(IEnumerable<string> roles, int? distributorId = null, CancellationToken cancellationToken = default);
    }
}
