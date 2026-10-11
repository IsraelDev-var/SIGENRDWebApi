using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Application;
using SIGENRD.Core.Application.Mediator;

namespace SIGENRD.UnitTests.Application;

/// <summary>COD-02: el mediador propio se comporta como MediatR en lo que usa el proyecto.</summary>
public class MediatorTests
{
    public record Ping(string Text) : IRequest<string>;

    public class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult("pong:" + request.Text);
    }

    public record Unhandled : IRequest<int>;

    public record WaitForCancel : IRequest<bool>;

    public class WaitForCancelHandler : IRequestHandler<WaitForCancel, bool>
    {
        public Task<bool> Handle(WaitForCancel request, CancellationToken cancellationToken) => Task.FromResult(cancellationToken.IsCancellationRequested);
    }

    public class Trace : List<string> { }

    public class OuterBehavior(Trace trace) : IPipelineBehavior<Ping, string>
    {
        public async Task<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            trace.Add("outer-in");
            var result = await next();
            trace.Add("outer-out");
            return "[" + result + "]";
        }
    }

    public class InnerBehavior(Trace trace) : IPipelineBehavior<Ping, string>
    {
        public async Task<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            trace.Add("inner-in");
            var result = await next(cancellationToken);
            trace.Add("inner-out");
            return result;
        }
    }

    private static ServiceProvider Build(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection().AddMediator(typeof(MediatorTests).Assembly);
        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Envia_la_peticion_a_su_handler()
    {
        using var services = Build();

        Assert.Equal("pong:hola", await services.GetRequiredService<IMediator>().Send(new Ping("hola")));
    }

    [Fact]
    public async Task Los_comportamientos_envuelven_al_handler_en_el_orden_de_registro()
    {
        var trace = new Trace();
        using var services = Build(s =>
        {
            s.AddSingleton(trace);
            s.AddTransient<IPipelineBehavior<Ping, string>, OuterBehavior>();
            s.AddTransient<IPipelineBehavior<Ping, string>, InnerBehavior>();
        });

        var result = await services.GetRequiredService<IMediator>().Send(new Ping("x"));

        Assert.Equal("[pong:x]", result);
        Assert.Equal(new[] { "outer-in", "inner-in", "inner-out", "outer-out" }, trace);
    }

    [Fact]
    public async Task Sin_handler_falla_con_un_mensaje_claro()
    {
        using var services = Build();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.GetRequiredService<IMediator>().Send(new Unhandled()));
        Assert.Contains(nameof(Unhandled), error.Message);
    }

    [Fact]
    public async Task Pasa_el_token_de_cancelacion_al_handler()
    {
        using var services = Build();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.True(await services.GetRequiredService<IMediator>().Send(new WaitForCancel(), cts.Token));
    }

    [Fact]
    public void La_capa_de_aplicacion_registra_un_handler_para_cada_peticion()
    {
        // Cada IRequest<T> del proyecto debe tener su handler: si falta uno, el endpoint fallaría en tiempo de ejecución.
        var assembly = typeof(SIGENRD.Core.Application.DependencyInjection).Assembly;

        var requests = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => (Type: t, Request: t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .Where(x => x.Request is not null)
            .ToList();

        Assert.NotEmpty(requests);
        var registered = new ServiceCollection().AddApplicationLayer();
        foreach (var (type, request) in requests)
        {
            var handler = typeof(IRequestHandler<,>).MakeGenericType(type, request!.GetGenericArguments()[0]);
            Assert.True(registered.Any(d => d.ServiceType == handler), "Falta el handler de " + type.Name);
        }
    }
}
