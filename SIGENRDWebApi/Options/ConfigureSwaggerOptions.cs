using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace SIGENRDWebApi.Options
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            // Crea un documento de swagger por cada versión de API descubierta
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = "EnergyMap API",
                Version = description.ApiVersion.ToString(),
                Description = "API de EnergyMap: mapa de disponibilidad, solicitudes de conexión y medición neta, simulación."
            };

            if (description.IsDeprecated)
            {
                info.Description += " ⚠️ Esta versión de la API está obsoleta.";
            }

            return info;
        }
    }
}
