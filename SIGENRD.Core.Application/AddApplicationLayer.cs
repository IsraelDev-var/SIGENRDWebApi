
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace SIGENRD.Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            // Automapper
            //services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // FluentValidation (si lo agregas más adelante)
            // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Servicios de dominio
            // services.AddScoped<IConnectionRequestService, ConnectionRequestService>();

            return services;
        }
    }
}
