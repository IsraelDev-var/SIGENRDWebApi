using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Configurations;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.Interfaces;
using SIGENRD.Infrastructure.Identity.JWT;
using SIGENRD.Infrastructure.Identity.Services;

using System.Text;

namespace SIGENRD.Infrastructure.Identity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {


            // ---------------------------------------------------------
            // 1. CONFIGURACIÓN DEL DB CONTEXT (CONDICIONAL)
            // ---------------------------------------------------------
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<IdentityDbContextSIGENRD>(options =>
                    options.UseInMemoryDatabase("SIGENRD_IdentityDB")); // Nombre único para Identity
            }
            else
            {
                services.AddDbContext<IdentityDbContextSIGENRD>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("IdentityServerDB")));
            }
            

                // 🔹 Configurar Identity Core
                services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    // Contraseña: 10 caracteres con mayúscula, minúscula y número; las comunes se rechazan aparte.
                    options.Password.RequiredLength = 10;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;

                    // Bloqueo ante fuerza bruta: 5 fallos seguidos bloquean la cuenta 15 minutos.
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<IdentityDbContextSIGENRD>()
                .AddDefaultTokenProviders()
                .AddPasswordValidator<Validators.CommonPasswordValidator>();

                // 🔹 Configurar JWT
                var jwtSettings = new JwtSettings();
                configuration.GetSection("JwtSettings").Bind(jwtSettings);
                // VALIDACIÓN DE SEGURIDAD
                // Sin emisor y audiencia los tokens se firman pero nunca se validan: mejor fallar al arrancar.
                if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
                    throw new InvalidOperationException("JwtSettings:Issuer y JwtSettings:Audience son obligatorios.");

                services.AddSingleton(jwtSettings);

                // RS256: la llave privada firma (JwtTokenGenerator) y solo la pública valida.
                var signingKeys = JwtSigningKeys.FromPrivateKey(jwtSettings.PrivateKey);
                services.AddSingleton(signingKeys);

                services.AddAuthentication(opt =>
                {
                    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(opt =>
                {
                    opt.RequireHttpsMetadata = false;
                    opt.SaveToken = true;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = signingKeys.ValidationKey,
                        // Solo RS256: evita que se acepte un token con otro algoritmo (alg=none, HS256 con la llave pública).
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                    };
                });

                // 🔹 JWT generator
                services.AddScoped<JwtTokenGenerator>();

                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<SIGENRD.Core.Application.Interfaces.Services.IStaffDirectory, StaffDirectory>();
            

                return services;
            
        }
    }
}
