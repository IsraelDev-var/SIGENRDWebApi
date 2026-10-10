

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
        ApprovalCertificate = 13,   // Certificado de aprobación (RN-14)
        ProofOfOwnership = 14,      // Título, alquiler u otro documento que acredite el inmueble
        OwnerAuthorization = 15,    // Autorización notariada del propietario, con su cédula
        TaxIdCard = 16,             // RNC
        ConstitutiveDocuments = 17, // Documentos constitutivos de la empresa
        BoardMinutes = 18,          // Acta del consejo
        IndustrialRegistry = 19,    // Registro industrial
        EngineerIdentityDocument = 20, // Identificación del ingeniero responsable
        DescriptiveMemory = 21,     // Memoria descriptiva del proyecto
        FeasibilityRequestForm = 22, // Formulario de factibilidad y punto de interconexión
        ApprovedPlans = 23,         // Planos sellados por el MOPC
        NoApprovedPlansLetter = 24, // Carta que indica que no posee planos del MOPC
        SitePlan = 25,              // Plano de situación (site plan)
        EquipmentCertificate = 26   // Certificado del equipo (IEEE 1547 o UL 1741)
    }
}
