using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Infrastructure.Shared;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Envía un correo real con SmtpEmailSender a un servidor SMTP de desarrollo (smtp4dev) y lo busca por su API.
/// Se activa con ENERGYMAP_TEST_SMTP="host;puertoSmtp;urlApi", por ejemplo "localhost;5025;http://localhost:5080".
/// Levantarlo: docker compose -f docker-compose.dev.yml up -d mail
/// </summary>
public class SmtpDeliveryTests
{
    private const string EnvironmentVariable = "ENERGYMAP_TEST_SMTP";

    private static readonly string? Setting = Environment.GetEnvironmentVariable(EnvironmentVariable);

    private sealed class FactIfSmtpAttribute : FactAttribute
    {
        public FactIfSmtpAttribute()
        {
            if (string.IsNullOrWhiteSpace(Setting))
                Skip = "Define " + EnvironmentVariable + " para probar el envío contra un servidor SMTP de desarrollo.";
        }
    }

    [FactIfSmtp]
    public async Task SmtpEmailSender_entrega_un_correo_con_remitente_asunto_y_cuerpo()
    {
        var parts = Setting!.Split(';');
        var host = parts[0];
        var port = parts[1];
        var api = parts[2].TrimEnd('/');

        // La configuración pasa por el mismo registro que usa la API (ServiceRegistration).
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MailSettings:EmailFrom"] = "no-reply@energymap.local",
            ["MailSettings:DisplayName"] = "EnergyMap (prueba)",
            ["MailSettings:SmtpHost"] = host,
            ["MailSettings:SmtpPort"] = port,
            ["MailSettings:UseStartTls"] = "false"
        }).Build();

        var services = new ServiceCollection().AddSharedInfrastructure(configuration).BuildServiceProvider();
        var settings = services.GetRequiredService<IOptions<SIGENRD.Core.Domain.Settings.MailSettings>>().Value;
        Assert.Equal("no-reply@energymap.local", settings.EmailFrom); // antes el enlace de configuración no se enlazaba

        var subject = "Prueba de entrega " + Guid.NewGuid().ToString("N")[..8];
        await services.GetRequiredService<IEmailSender>().SendAsync(new EmailRequest
        {
            To = "destino@test.energymap.local",
            Subject = subject,
            Body = "<p>Hola desde la prueba</p>"
        });

        using var http = new HttpClient();
        using var doc = JsonDocument.Parse(await http.GetStringAsync(api + "/api/messages"));

        var message = doc.RootElement.GetProperty("results").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("subject").GetString() == subject);

        Assert.True(message.ValueKind == JsonValueKind.Object, "el correo no llegó al servidor SMTP de desarrollo");
        Assert.Contains("no-reply@energymap.local", message.GetProperty("from").GetString());
        Assert.Contains("destino@test.energymap.local", message.GetProperty("to").EnumerateArray().First().GetString());
    }
}
