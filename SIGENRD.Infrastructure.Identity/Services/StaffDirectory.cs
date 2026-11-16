using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Services
{
    public class StaffDirectory(UserManager<ApplicationUser> userManager) : IStaffDirectory
    {
        public async Task<IReadOnlyList<string>> GetUserIdsInRolesAsync(
            IEnumerable<string> roles, int? distributorId = null, CancellationToken cancellationToken = default)
        {
            var ids = new HashSet<string>();

            foreach (var role in roles.Distinct())
            {
                var users = await userManager.GetUsersInRoleAsync(role);
                foreach (var user in users)
                {
                    // El personal de otra EDE no se entera de lo que no puede ver; el administrador, de todo.
                    if (distributorId.HasValue && role != nameof(UserType.Admin) && user.DistributorId != distributorId)
                        continue;
                    ids.Add(user.Id);
                }
            }

            return ids.ToList();
        }
    }
}
