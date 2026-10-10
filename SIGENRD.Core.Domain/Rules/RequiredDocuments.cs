using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules
{
    /// <summary>
    /// Documentos mínimos para pasar a revisión y para aprobar (RN-09, RN-11).
    /// Medición neta: los cuatro de RN-11. Conexión estándar: la lista de RN-09 traducida a tipos de documento
    /// (plano, identificación del cliente y carta de solicitud); es un supuesto a confirmar con Edesur.
    /// </summary>
    public static class RequiredDocuments
    {
        public static IReadOnlyList<DocumentType> For(RequestType requestType) => requestType switch
        {
            RequestType.NetMetering => new[]
            {
                DocumentType.Blueprint,              // plano unifilar
                DocumentType.InterconnectionScheme,  // esquema de interconexión
                DocumentType.InverterSpecification,  // especificaciones del inversor
                DocumentType.CalculationMemory       // memoria de cálculo
            },
            _ => new[]
            {
                DocumentType.Blueprint,
                DocumentType.IdentityDocument,
                DocumentType.RequestLetter
            }
        };

        public static IReadOnlyList<DocumentType> Missing(RequestType requestType, IEnumerable<DocumentType> present)
        {
            var have = present.ToHashSet();
            return For(requestType).Where(required => !have.Contains(required)).ToList();
        }
    }
}
