using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Recorremos todos los valores del Enum UserType
            foreach (UserType roleName in Enum.GetValues(typeof(UserType)))
            {
                var roleString = roleName.ToString();

                // Si el rol no existe, lo creamos
                if (!await roleManager.RoleExistsAsync(roleString))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleString,
                        Description = $"Rol por defecto para {roleString}"
                    });
                }
            }
        }
    }
}