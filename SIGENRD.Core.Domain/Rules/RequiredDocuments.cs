using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules
{
    /// <summary>
    /// Un requisito documental. Se cumple con cualquiera de los tipos de AnyOf
    /// (por ejemplo, "título de propiedad" o "autorización del dueño").
    /// </summary>
    public record DocumentRequirement(string Code, string Description, IReadOnlyList<DocumentType> AnyOf)
    {
        public DocumentRequirement(string code, string description, params DocumentType[] anyOf)
            : this(code, description, (IReadOnlyList<DocumentType>)anyOf) { }

        public bool IsSatisfiedBy(IReadOnlySet<DocumentType> present) => AnyOf.Any(present.Contains);
    }

    /// <summary>
    /// Documentos mínimos para pasar a revisión, reenviar y aprobar (RN-09, RN-11).
    ///
    /// Fuentes (consultadas en octubre de 2026; confirmar vigencia con Edesur):
    ///  - Conexión de hasta 10 kVA: requisitos de contrato de Edenorte, EdeEste y Edesur.
    ///  - Conexión de más de 10 kVA: proyecto de ingeniería (Edenorte, "mayor a 10 kVA").
    ///  - Medición neta: Reglamento de Interconexión de Generación Distribuida (CNE, 2012), art. 14 y anexos A y C.
    ///
    /// La memoria de cálculo y el esquema de interconexión de la versión 1.2 del análisis no figuran en el
    /// reglamento: se aceptan como documentos opcionales, pero no son obligatorios.
    /// </summary>
    public static class RequiredDocuments
    {
        /// <summary>Hasta esta potencia la conexión es un trámite de contrato; por encima exige proyecto de ingeniería.</summary>
        public const decimal DirectContractMaxKva = 10m;

        public static IReadOnlyList<DocumentRequirement> For(ConnectionRequest request, bool isLegalEntity)
        {
            var list = new List<DocumentRequirement>();

            if (request.RequestType == RequestType.NetMetering)
                AddInterconnection(list, request);
            else if (request.RequestedCapacityKva > DirectContractMaxKva)
                AddEngineeringProject(list);
            else
                AddContract(list, request, isLegalEntity);

            return list;
        }

        public static IReadOnlyList<DocumentRequirement> Missing(
            ConnectionRequest request, bool isLegalEntity, IEnumerable<DocumentType> present)
        {
            var have = present.ToHashSet();
            return For(request, isLegalEntity).Where(r => !r.IsSatisfiedBy(have)).ToList();
        }

        // ---- Conexión de hasta 10 kVA: contrato de suministro ----
        private static void AddContract(List<DocumentRequirement> list, ConnectionRequest request, bool isLegalEntity)
        {
            list.Add(new("IDENTIDAD", "Cédula o pasaporte del solicitante" + (isLegalEntity ? " (representante legal)" : ""),
                DocumentType.IdentityDocument));

            list.Add(new("INMUEBLE", "Título, contrato de alquiler u otro documento que acredite el inmueble, o autorización del propietario",
                DocumentType.ProofOfOwnership, DocumentType.OwnerAuthorization));

            if (!isLegalEntity) return;

            list.Add(new("CARTA", "Carta timbrada de la empresa solicitando el servicio", DocumentType.RequestLetter));
            list.Add(new("RNC", "Copia del RNC", DocumentType.TaxIdCard));
            list.Add(new("REGISTRO_MERCANTIL", "Registro Mercantil vigente", DocumentType.CompanyRegistration));
            list.Add(new("CONSTITUTIVOS", "Documentos constitutivos de la empresa", DocumentType.ConstitutiveDocuments));
            list.Add(new("ACTA", "Acta del consejo que avala al representante legal", DocumentType.BoardMinutes));

            // Edesur: obligatorio para la tarifa industrial.
            if (request.UsageType == UsageType.Industrial)
                list.Add(new("REGISTRO_INDUSTRIAL", "Registro industrial (tarifa industrial)", DocumentType.IndustrialRegistry));
        }

        // ---- Conexión de más de 10 kVA: proyecto de ingeniería ----
        private static void AddEngineeringProject(List<DocumentRequirement> list)
        {
            list.Add(new("CARTA_INGENIERO", "Carta del cliente que autoriza al ingeniero responsable", DocumentType.RequestLetter));
            list.Add(new("IDENTIDAD", "Cédula o pasaporte del cliente", DocumentType.IdentityDocument));
            list.Add(new("CODIA", "Carnet CODIA del ingeniero responsable", DocumentType.CODIALicense));
            list.Add(new("IDENTIDAD_INGENIERO", "Identificación del ingeniero", DocumentType.EngineerIdentityDocument));
            list.Add(new("MEMORIA", "Memoria descriptiva del proyecto", DocumentType.DescriptiveMemory));
            list.Add(new("FACTIBILIDAD", "Formulario de solicitud de estudio de factibilidad y punto de interconexión",
                DocumentType.FeasibilityRequestForm));
            list.Add(new("PLANOS", "Planos eléctricos de interconexión", DocumentType.Blueprint));
            list.Add(new("PLANOS_MOPC", "Planos sellados por el MOPC, o carta indicando que no los posee",
                DocumentType.ApprovedPlans, DocumentType.NoApprovedPlansLetter));
        }

        // ---- Medición neta: solicitud de evaluación de interconexión (CNE 2012, art. 14) ----
        private static void AddInterconnection(List<DocumentRequirement> list, ConnectionRequest request)
        {
            list.Add(new("IDENTIDAD", "Cédula del solicitante", DocumentType.IdentityDocument));
            list.Add(new("PLANO_SITUACION", "Plano de situación (site plan) del proyecto", DocumentType.SitePlan));
            list.Add(new("UNIFILAR", "Diagrama unifilar con el esquema de protección y control, sus funciones y ajustes",
                DocumentType.Blueprint));
            list.Add(new("EQUIPOS", "Listado de equipos propuestos y ficha del inversor (certificación IEEE 1547 o UL 1741)",
                DocumentType.InverterSpecification));

            // Anexo A, parte C: si el solicitante no es el dueño, se aportan los datos y la cédula del dueño.
            if (request.PropertyRelation != PropertyRelation.Owner)
                list.Add(new("DUENO", "Autorización y cédula del dueño del inmueble", DocumentType.OwnerAuthorization));
        }
    }
}
