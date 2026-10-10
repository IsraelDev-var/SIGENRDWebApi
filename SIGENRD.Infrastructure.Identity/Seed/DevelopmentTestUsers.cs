using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Seeds
{
    /// <summary>
    /// Usuarios de prueba, uno por rol, para validar la autorización a mano o con pruebas automatizadas.
    /// Solo se siembran en Development y solo si Seed:TestUsersPassword está configurada.
    /// El administrador sale de DefaultSuperAdmin.
    /// </summary>
    public static class DevelopmentTestUsers
    {
        public const string Domain = "test.energymap.local";

        public static readonly IReadOnlyList<(string Email, string FullName, UserType UserType)> Users = new[]
        {
            ("customer@" + Domain, "Cliente de prueba", UserType.Customer),
            ("installer@" + Domain, "Instaladora de prueba", UserType.InstallerCompany),
            ("engineer@" + Domain, "Ingeniero de prueba", UserType.Engineer),
            ("distributor@" + Domain, "Distribuidora de prueba", UserType.Distributor),
        };

        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return;

            foreach (var (email, fullName, userType) in Users)
            {
                if (await userManager.FindByEmailAsync(email) is not null)
                    continue;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    IsEmailConfirmed = true,
                    UserType = userType
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, userType.ToString());
                else
                    Console.WriteLine("No se pudo crear el usuario de prueba " + email + ": " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
