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

            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}