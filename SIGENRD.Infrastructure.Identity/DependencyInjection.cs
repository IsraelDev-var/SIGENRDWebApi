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
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = false;
                    options.User.RequireUniqueEmail = true;

                    // Bloqueo ante fuerza bruta: 5 fallos seguidos bloquean la cuenta 15 minutos.
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<IdentityDbContextSIGENRD>()
                .AddDefaultTokenProviders();

                // 🔹 Configurar JWT
                var jwtSettings = new JwtSettings();
                configuration.GetSection("JwtSettings").Bind(jwtSettings);
                // VALIDACIÓN DE SEGURIDAD
                if (string.IsNullOrEmpty(jwtSettings.Key))
                    throw new Exception("JwtSettings:Key no está configurada en appsettings.json");
                // Sin emisor y audiencia los tokens se firman pero nunca se validan: mejor fallar al arrancar.
                if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
                    throw new InvalidOperationException("JwtSettings:Issuer y JwtSettings:Audience son obligatorios.");

                services.AddSingleton(jwtSettings);

                // La misma llave (Base64 decodificada) que usa JwtTokenGenerator para firmar.
                var signingKey = JwtTokenGenerator.BuildSecurityKey(jwtSettings.Key);

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
                        IssuerSigningKey = signingKey
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
