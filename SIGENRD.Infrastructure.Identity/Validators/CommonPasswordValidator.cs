using Microsoft.AspNetCore.Identity;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Validators
{
    /// <summary>
    /// Rechaza contraseñas que cumplen las reglas de forma pero son fáciles de adivinar:
    /// una palabra común con números o símbolos alrededor (Password2026, Edesur12345) o el propio correo.
    /// </summary>
    public class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        // Palabras base de las contraseñas más usadas, más las propias del dominio del sistema.
        private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passw", "contraseña", "contrasena", "clave", "qwerty", "qwertyuiop", "asdfgh", "zxcvbn",
            "abc", "abcdef", "admin", "administrador", "welcome", "bienvenido", "letmein", "iloveyou", "teamo",
            "monkey", "dragon", "football", "futbol", "baseball", "beisbol", "superman", "batman", "master",
            "sunshine", "princess", "princesa", "secret", "secreto", "usuario", "user", "test", "prueba",
            "dominicana", "republica", "santodomingo", "edesur", "edenorte", "edeeste", "energymap", "sigenrd",
            "energia", "solar", "cambiame", "changeme"
        };

        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
        {
            if (string.IsNullOrEmpty(password))
                return Task.FromResult(IdentityResult.Success);

            // El núcleo de letras, sin los números ni símbolos que se suelen añadir para cumplir las reglas.
            var core = new string(password.Where(char.IsLetter).ToArray());

            if (core.Length == 0 || CommonWords.Contains(core))
                return Failed("PasswordTooCommon", "La contraseña es demasiado común. Elija una frase o combinación menos predecible.");

            var emailName = user.Email?.Split('@')[0];
            if (!string.IsNullOrEmpty(emailName) && emailName.Length >= 4
                && password.Contains(emailName, StringComparison.OrdinalIgnoreCase))
                return Failed("PasswordContainsEmail", "La contraseña no puede contener su correo.");

            return Task.FromResult(IdentityResult.Success);
        }

        private static Task<IdentityResult> Failed(string code, string description)
            => Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code, Description = description }));
    }
}
