using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIGENRD.Infrastructure.Identity.Configurations
{
    /// <summary>
    /// Permite generar migraciones y scripts de Identity sin arrancar el host web (dotnet ef, CI/CD).
    /// La cadena se toma de ConnectionStrings__IdentityServerDB; para generar scripts no hace falta una base viva.
    /// </summary>
    public class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContextSIGENRD>
    {
        public IdentityDbContextSIGENRD CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IdentityServerDB")
                ?? "Host=localhost;Port=5432;Database=SIGENRD;Username=postgres;Password=design-time";

            var options = new DbContextOptionsBuilder<IdentityDbContextSIGENRD>()
                .UseNpgsql(connectionString)
                .Options;

            return new IdentityDbContextSIGENRD(options);
        }
    }
}
