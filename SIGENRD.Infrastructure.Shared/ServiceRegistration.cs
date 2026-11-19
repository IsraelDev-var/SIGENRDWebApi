using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Settings;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.Infrastructure.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

            services.Configure<EmailOutboxSettings>(configuration.GetSection(EmailOutboxSettings.SectionName));

            // Documentos de los expedientes (RF-35) y certificado de aprobación (RN-14).
            services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
            var storageProvider = configuration[StorageSettings.SectionName + ":Provider"] ?? "AzureBlob";
            if (string.Equals(storageProvider, "Local", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IFileStorage, LocalFileStorage>();
            else if (string.Equals(storageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
            else
                throw new InvalidOperationException("Storage:Provider debe ser AzureBlob o Local, no '" + storageProvider + "'.");

            services.AddSingleton<ICertificateGenerator, ApprovalCertificateGenerator>();
            services.AddSingleton<SIGENRD.Core.Application.Features.Simulations.ISimulationReportGenerator, SimulationReportGenerator>();

            // Reportes exportables (RF-30)
            services.AddSingleton<SIGENRD.Core.Application.Features.Reports.IReportExporter, XlsxReportExporter>();
            services.AddSingleton<SIGENRD.Core.Application.Features.Reports.IReportExporter, PdfReportExporter>();

            // Proveedor de correo: SMTP (por defecto) o la API HTTPS de Brevo.
            var mailProvider = configuration["MailSettings:Provider"] ?? "Smtp";
            Func<IServiceProvider, IEmailSender> sender;
            if (string.Equals(mailProvider, "Brevo", StringComparison.OrdinalIgnoreCase))
            {
                // Un solo HttpClient para toda la app; se renuevan las conexiones para respetar cambios de DNS.
                services.AddSingleton(sp => new BrevoEmailSender(
                    new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    },
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailSettings>>()));
                sender = sp => sp.GetRequiredService<BrevoEmailSender>();
            }
            else if (string.Equals(mailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                services.AddTransient<SmtpEmailSender>();
                sender = sp => sp.GetRequiredService<SmtpEmailSender>();
            }
            else
                throw new InvalidOperationException("MailSettings:Provider debe ser Smtp o Brevo, no '" + mailProvider + "'.");

            services.AddTransient(sender);

            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                // Sin base relacional no hay bandeja de salida: envío directo, como antes.
                services.AddTransient<IEmailService>(sp => (IEmailService)sender(sp));
                return services;
            }

            // SCA-01: la petición solo deja el correo en la bandeja; el despachador lo envía y reintenta.
            services.AddSingleton<EmailOutboxSignal>();
            services.AddScoped<IEmailService, OutboxEmailService>();
            services.AddSingleton<EmailOutboxDispatcher>();
            services.AddHostedService(sp => sp.GetRequiredService<EmailOutboxDispatcher>());

            return services;
        }
    }
}