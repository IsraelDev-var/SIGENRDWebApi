using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Application;
using SIGENRD.Infrastructure.Identity;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.Seeds;
using SIGENRD.Infrastructure.Persistences;
using SIGENRD.Presentation.WebApi;

var builder = WebApplication.CreateBuilder(args);

// ===========================================================
// 🧱 REGISTRO DE CAPAS
// ===========================================================
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
// Aquí ya se configura Controllers, Versioning y Swagger gracias al paso 2
builder.Services.AddPresentationLayer();

// HealthChecks
builder.Services.AddHealthChecks();

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
        await DefaultRoles.SeedAsync(userManager, roleManager);
        await DefaultSuperAdmin.SeedAsync(userManager, roleManager);
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

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.UseHealthChecks("/health");

try
{
    app.MapControllers(); // 👈 Aquí está el error
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