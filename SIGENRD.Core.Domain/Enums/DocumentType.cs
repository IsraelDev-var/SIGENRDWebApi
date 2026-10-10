

namespace SIGENRD.Core.Domain.Enums
{
    public enum DocumentType
    {
        Blueprint = 1,          // Plano unifilar
        TechnicalSheet = 2,     // Ficha técnica
        Invoice = 3,            // Factura eléctrica
        IdentityDocument = 4,   // Cédula o RNC
        CODIALicense = 5,       // Carnet CODIA
        CompanyRegistration = 6, // Registro mercantil
        RequestLetter = 7,      // Carta de solicitud
        Permit = 8,             // Permiso o autorización
        Other = 9,              // Cualquier otro documento
        InterconnectionScheme = 10, // Esquema de interconexión (RN-11)
        InverterSpecification = 11, // Especificaciones del inversor (RN-11)
        CalculationMemory = 12,     // Memoria de cálculo (RN-11)
        ApprovalCertificate = 13    // Certificado de aprobación (RN-14)
    }
}
