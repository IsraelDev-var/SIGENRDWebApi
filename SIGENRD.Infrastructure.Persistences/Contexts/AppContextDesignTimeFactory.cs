using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIGENRD.Infrastructure.Persistences.Contexts
{
    /// <summary>
    /// Permite crear migraciones sin arrancar el host web. Solo se usa en tiempo de diseño (dotnet ef).
    /// La cadena real se toma de la variable de entorno ConnectionStrings__Postgres si existe;
    /// para generar migraciones no hace falta una base de datos viva.
    /// </summary>
    public class AppContextDesignTimeFactory : IDesignTimeDbContextFactory<AppContextSIGENRD>
    {
        public AppContextSIGENRD CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? "Host=localhost;Port=5432;Database=SIGENRD;Username=postgres;Password=design-time";

            var options = new DbContextOptionsBuilder<AppContextSIGENRD>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
                .Options;

            return new AppContextSIGENRD(options);
        }
    }
}
