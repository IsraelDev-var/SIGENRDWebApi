using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Application;
using SIGENRD.Infrastructure.Identity;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.Seeds;
using SIGENRD.Infrastructure.Persistences;
using SIGENRD.Presentation.WebApi;
using SIGENRD.Infrastructure.Shared;
using SIGENRDWebApi.Middlewares;
using SIGENRDWebApi.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using SIGENRDWebApi.HealthChecks;
using SIGENRD.Infrastructure.Identity.Configurations;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRDWebApi.Observability;
using SIGENRDWebApi.Configuration;
using SIGENRD.Infrastructure.Persistences.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Secretos desde Azure Key Vault si se configura KeyVault:Uri (producción). Antes de que nada lea la configuración.
builder.Configuration.AddKeyVaultIfConfigured();

// No anunciar el servidor en la cabecera Server.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// 1. Serilog (lo primero de todo): texto en desarrollo, JSON por stdout fuera de él.
builder.Host.UseSerilog(ObservabilityExtensions.ConfigureSerilog);

// Trazas y métricas por OpenTelemetry, si hay destino OTLP configurado.
builder.Services.AddObservability(builder.Configuration);

// ===========================================================
// 🧱 REGISTRO DE CAPAS
// ===========================================================
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Aquí ya se configura Controllers, Versioning y Swagger gracias al paso 2
builder.Services.AddPresentationLayer(builder.Configuration);

// HealthChecks: /health comprueba las bases de datos (listo para atender); /health/live solo el proceso.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck<AppContextSIGENRD>>("database")
    .AddCheck<DatabaseHealthCheck<IdentityDbContextSIGENRD>>("identity-database");
// ===========================================================
// 🧱 PIPELINE
// ===========================================================
var app = builder.Build();

// ===========================================================
// 🌱 DATABASE SEEDING
// ===========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ejecutar los Seeds
        var configuration = services.GetRequiredService<IConfiguration>();

        await DefaultRoles.SeedAsync(userManager, roleManager);
        await DefaultSuperAdmin.SeedAsync(userManager, roleManager, configuration["Seed:AdminPassword"]);

        // Usuarios de prueba por rol: solo en desarrollo
        if (app.Environment.IsDevelopment())
            await DevelopmentTestUsers.SeedAsync(userManager, configuration["Seed:TestUsersPassword"]);

        // Transformadores de demostración para ver el mapa en desarrollo y en las pruebas E2E (Seed:DemoTransformers).
        if (app.Environment.IsDevelopment())
            await DevelopmentDemoData.SeedAsync(
                services.GetRequiredService<AppContextSIGENRD>(), configuration.GetValue("Seed:DemoTransformers", 0));
    }
    catch (Exception ex)
    {
        // Opcional: Loggear si algo falla en el seed
        Console.WriteLine($"Ocurrió un error en el Seeding: {ex.Message}");
    }
}


// Configuración del Middleware de Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        try
        {
            // Esto genera un dropdown en la UI para elegir entre v1, v2, etc.
            var descriptions = app.DescribeApiVersions();
            foreach (var description in descriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocurrió un error al configurar Swagger UI: {ex.Message}");
        }

    });
}

// 1. Cabeceras de seguridad en todas las respuestas, incluidas las de error
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Registrar Middleware de Errores Globales
app.UseMiddleware<ErrorHandlerMiddleware>();

// 3. Registrar Logging de Peticiones HTTP (Importante para ver GET/POST en logs)
app.UseSerilogRequestLogging(options => options.GetLevel = ObservabilityExtensions.RequestLogLevel);

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

try
{
    app.MapControllers(); 
}
catch (System.Reflection.ReflectionTypeLoadException ex)
{
    // Esto imprimirá en la consola negra el error REAL
    foreach (var loaderException in ex.LoaderExceptions)
    {
        Console.WriteLine($"🚨 ERROR REAL: {loaderException?.Message}");
    }
    throw; // Detiene la app
}
app.Run();

// Permite que las pruebas de integración arranquen la aplicación con WebApplicationFactory<Program>.
public partial class Program { }
