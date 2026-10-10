using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Seeds
{
    public static class DefaultSuperAdmin
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, string? password)
        {
            // Sin contraseña configurada no se crea el administrador: no hay credenciales por defecto.
            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Seed:AdminPassword no está configurada: no se crea el administrador por defecto.");
                return;
            }

            // Definir el usuario Admin por defecto
            var defaultUser = new ApplicationUser
            {
                UserName = "admin@sigenrd.com",
                Email = "admin@sigenrd.com",
                FullName = "Administrador del Sistema",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                UserType = UserType.Admin, // Asegúrate de tener este valor en tu Enum
                IsEmailConfirmed = true
            };

            // Verificar si el usuario ya existe por email
            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    // Crear usuario con contraseña segura
                    var result = await userManager.CreateAsync(defaultUser, password);

                    if (result.Succeeded)
                    {
                        // Asignarle el rol de Admin
                        await userManager.AddToRoleAsync(defaultUser, UserType.Admin.ToString());
                    }
                }
            }
        }
    }
}