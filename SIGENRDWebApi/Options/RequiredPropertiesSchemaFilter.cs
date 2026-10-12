using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIGENRDWebApi.Options
{
    /// <summary>
    /// Marca como requeridas en el contrato OpenAPI las propiedades que no admiten null. La API siempre las envía,
    /// así que los tipos generados del frontend no las declaran opcionales (sin esto, todo campo era "x?: tipo").
    /// </summary>
    public class RequiredPropertiesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties is null || schema.Properties.Count == 0) return;

            foreach (var (name, property) in schema.Properties)
            {
                if (!property.Nullable) schema.Required.Add(name);
            }
        }
    }
}
