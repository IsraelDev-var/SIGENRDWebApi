using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// Traduce el rol del usuario actual al alcance de solicitudes que puede ver (RN-22).
    /// Los nombres de rol coinciden con los de UserType, que es de donde se siembran.
    /// </summary>
    public static class RequestScopeResolver
    {
        public static RequestScope For(ICurrentUserService user)
        {
            if (user.IsInRole(nameof(UserType.Admin)))
                return new RequestScope(RequestScopeKind.All);

            // El personal de una EDE solo ve las solicitudes de su EDE; sin EDE asignada no ve ninguna.
            if (user.IsInRole(nameof(UserType.Distributor)) || user.IsInRole(nameof(UserType.Engineer)))
                return user.DistributorId is int distributorId
                    ? new RequestScope(RequestScopeKind.All, DistributorId: distributorId)
                    : new RequestScope(RequestScopeKind.None);

            if (user.IsInRole(nameof(UserType.Customer)))
                return new RequestScope(RequestScopeKind.Customer, user.UserId);

            if (user.IsInRole(nameof(UserType.InstallerCompany)))
                return new RequestScope(RequestScopeKind.InstallerCompany, user.UserId);

            return new RequestScope(RequestScopeKind.None);
        }
    }
}
