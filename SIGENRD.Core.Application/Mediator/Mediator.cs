using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SIGENRD.Core.Application.Mediator
{
    // Mediador propio (COD-02): reemplaza a MediatR, cuya versión 13 tiene licencia comercial.
    // Mantiene los mismos nombres para que handlers, comportamientos y controladores no cambien.

    /// <summary>Petición (comando o consulta) que devuelve <typeparamref name="TResponse"/>.</summary>
    public interface IRequest<out TResponse> { }

    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>Siguiente paso de la cadena: otro comportamiento o el handler.</summary>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

    /// <summary>Paso que envuelve a todos los handlers (validación, etc.), en el orden en que se registran.</summary>
    public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }

    public class Mediator(IServiceProvider services) : IMediator
    {
        // Un despachador tipado por tipo de petición, creado una sola vez.
        private static readonly ConcurrentDictionary<Type, object> Dispatchers = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var dispatcher = (Dispatcher<TResponse>)Dispatchers.GetOrAdd(request.GetType(), requestType =>
                Activator.CreateInstance(typeof(Dispatcher<,>).MakeGenericType(requestType, typeof(TResponse)))!);

            return dispatcher.Send(request, services, cancellationToken);
        }

        private abstract class Dispatcher<TResponse>
        {
            public abstract Task<TResponse> Send(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);
        }

        private sealed class Dispatcher<TRequest, TResponse> : Dispatcher<TResponse> where TRequest : IRequest<TResponse>
        {
            public override Task<TResponse> Send(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken)
            {
                var typed = (TRequest)request;
                var handler = services.GetService<IRequestHandler<TRequest, TResponse>>()
                    ?? throw new InvalidOperationException($"No hay handler registrado para {typeof(TRequest).Name}.");

                RequestHandlerDelegate<TResponse> next = ct => handler.Handle(typed, ct);

                // El primer comportamiento registrado queda por fuera de todos.
                foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
                {
                    var inner = next;
                    next = ct => behavior.Handle(typed, inner, ct);
                }

                return next(cancellationToken);
            }
        }
    }

    public static class MediatorRegistration
    {
        /// <summary>Registra el mediador y todos los handlers concretos del ensamblado.</summary>
        public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
        {
            services.AddTransient<IMediator, Mediator>();

            var handlers = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .Select(i => (Service: i, Implementation: t)));

            foreach (var (service, implementation) in handlers)
                services.AddTransient(service, implementation);

            return services;
        }
    }
}
