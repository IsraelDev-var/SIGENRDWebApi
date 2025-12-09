
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Application.Behaviours;
using SIGENRD.Core.Application.Mappings;
using System.Reflection;



namespace SIGENRD.Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            // Automapper

            services.AddAutoMapper(typeof(GeneralProfile).Assembly);
            // 2. MediatR (⚡ NUEVO)
            // Esto escanea todo el proyecto en busca de IRequest e IRequestHandler
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // 3. FluentValidation (⚡ NUEVO - Lo usaremos pronto)
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            // 4. ⚡ REGISTRAR EL PIPELINE DE VALIDACIÓN 
            // Esto conecta el ValidationBehavior con MediatR
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Servicios de aplicación

            return services;
        }
    }
}
