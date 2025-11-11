using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppContextSIGENRD>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("Postgres"),
                    npgsql => npgsql.UseNetTopologySuite()
                ));

            // Repositorios
            // services.AddScoped<ICustomerRepository, CustomerRepository>();
            // services.AddScoped<IConnectionRequestRepository, ConnectionRequestRepository>();

            return services;
        }
    }
}
