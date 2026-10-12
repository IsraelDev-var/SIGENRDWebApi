using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIGENRD.Infrastructure.Persistences.Caching;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;


namespace SIGENRD.Infrastructure.Persistences
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Caché del mapa (RNF-01): copia en memoria de los transformadores, invalidada al guardar cambios en ellos.
            var mapCache = configuration.GetSection(MapCacheSettings.SectionName).Get<MapCacheSettings>() ?? new MapCacheSettings();
            services.AddSingleton(mapCache);
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<TransformerMapCache>();
            services.AddSingleton<TransformerChangeInterceptor>();

            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<AppContextSIGENRD>((sp, options) => options
                    .UseInMemoryDatabase("SIGENRD_AppDB") // 👈 Nombre diferente al de Identity
                    .AddInterceptors(sp.GetRequiredService<TransformerChangeInterceptor>()));
            }
            else
            {
                services.AddDbContext<AppContextSIGENRD>((sp, options) => options
                   .UseNpgsql(
                       configuration.GetConnectionString("Postgres"),
                       npgsql => npgsql.UseNetTopologySuite())
                   .AddInterceptors(sp.GetRequiredService<TransformerChangeInterceptor>()));
            }


            // Repositorios
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<TransformerRepository>();
            services.AddScoped<ITransformerRepository, CachedTransformerRepository>();
            services.AddScoped<IConnectionRequestRepository, ConnectionRequestRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInstallerCompanyRepository, InstallerCompanyRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISimulationRepository, SimulationRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<SIGENRD.Core.Domain.Interfaces.ISecurityAuditLog, Services.SecurityAuditLog>();

            return services;
        }
    }
}
