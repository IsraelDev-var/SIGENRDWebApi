using System.Reflection;
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
        private static readonly NullabilityInfoContext Nullability = new();

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties is null || schema.Properties.Count == 0) return;

            foreach (var (name, property) in schema.Properties)
            {
                if (property.Nullable) continue;
                // Una referencia a otro esquema ($ref) no lleva "nullable": se mira la anotación de C# (Holder?, NetMetering?).
                if (property.Reference is not null && IsNullableMember(context.Type, name)) continue;
                schema.Required.Add(name);
            }
        }

        private static bool IsNullableMember(Type type, string jsonName)
        {
            var member = type.GetProperty(jsonName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return member is not null && Nullability.Create(member).ReadState == NullabilityState.Nullable;
        }
    }
}
