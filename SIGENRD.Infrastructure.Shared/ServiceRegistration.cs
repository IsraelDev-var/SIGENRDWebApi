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

            services.AddTransient<SmtpEmailSender>();
            services.AddTransient<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());

            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                // Sin base relacional no hay bandeja de salida: envío directo, como antes.
                services.AddTransient<IEmailService>(sp => sp.GetRequiredService<SmtpEmailSender>());
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