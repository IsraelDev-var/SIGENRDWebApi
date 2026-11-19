using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Domain.Settings;
using SIGENRD.Infrastructure.Shared;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Application;

public class BrevoEmailSenderTests
{
    private sealed class CaptureHandler(HttpStatusCode status, string body = "{}") : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Content { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Content = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }

    private static BrevoEmailSender Sender(CaptureHandler handler, string apiKey = "clave-de-prueba") =>
        new(new HttpClient(handler), Options.Create(new MailSettings
        {
            Provider = "Brevo",
            BrevoApiKey = apiKey,
            EmailFrom = "remitente@example.com",
            DisplayName = "Equipo SIGENRD"
        }));

    [Fact]
    public async Task Envia_el_correo_a_la_API_de_Brevo_con_la_clave_y_el_remitente_configurado()
    {
        var handler = new CaptureHandler(HttpStatusCode.Created, "{\"messageId\":\"1\"}");

        await Sender(handler).SendAsync(new EmailRequest { To = "persona@example.com", Subject = "Confirma", Body = "<p>Hola</p>" });

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(BrevoEmailSender.Endpoint, handler.Request.RequestUri!.ToString());
        Assert.Equal("clave-de-prueba", handler.Request.Headers.GetValues("api-key").Single());

        using var json = JsonDocument.Parse(handler.Content!);
        var root = json.RootElement;
        Assert.Equal("remitente@example.com", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Equipo SIGENRD", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("persona@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Confirma", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Hola</p>", root.GetProperty("htmlContent").GetString());
    }

    [Fact]
    public async Task Una_respuesta_de_error_de_Brevo_lanza_para_que_la_bandeja_reintente()
    {
        var handler = new CaptureHandler(HttpStatusCode.Unauthorized, "{\"code\":\"unauthorized\"}");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Sender(handler).SendAsync(new EmailRequest { To = "persona@example.com", Subject = "s", Body = "b" }));

        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task Sin_clave_no_llama_a_Brevo()
    {
        var handler = new CaptureHandler(HttpStatusCode.Created);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Sender(handler, apiKey: "").SendAsync(new EmailRequest { To = "persona@example.com", Subject = "s", Body = "b" }));

        Assert.Null(handler.Request);
    }

    [Theory]
    [InlineData("Brevo", typeof(BrevoEmailSender))]
    [InlineData("Smtp", typeof(SmtpEmailSender))]
    [InlineData(null, typeof(SmtpEmailSender))]
    public void El_proveedor_configurado_elige_el_emisor(string? provider, Type expected)
    {
        var settings = new Dictionary<string, string?> { ["UseInMemoryDatabase"] = "true" };
        if (provider is not null) settings["MailSettings:Provider"] = provider;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSharedInfrastructure(configuration);
        using var sp = services.BuildServiceProvider();

        Assert.IsType(expected, sp.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Un_proveedor_desconocido_detiene_el_arranque()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MailSettings:Provider"] = "Postal" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSharedInfrastructure(configuration));
    }
}
