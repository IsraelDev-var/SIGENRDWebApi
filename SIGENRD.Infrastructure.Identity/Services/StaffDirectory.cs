using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Services
{
    public class StaffDirectory(UserManager<ApplicationUser> userManager) : IStaffDirectory
    {
        public async Task<IReadOnlyList<string>> GetUserIdsInRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            var ids = new HashSet<string>();

            foreach (var role in roles.Distinct())
            {
                var users = await userManager.GetUsersInRoleAsync(role);
                foreach (var user in users)
                    ids.Add(user.Id);
            }

            return ids.ToList();
        }
    }
}
