using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.Core.Application.Features.Catalogs
{
    /// <summary>Un documento que pide el trámite; cualquiera de los tipos aceptados lo cumple.</summary>
    public class DocumentRequirementDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AcceptedTypes { get; set; } = new();
    }

    /// <summary>
    /// Requisitos de un trámite antes de crear la solicitud (B-4, §3.3 "consulta de requisitos de conexión").
    /// Usa la misma regla que valida los expedientes (RequiredDocuments, RN-09 y RN-11): la página pública y la
    /// revisión no pueden contradecirse.
    /// </summary>
    public class GetDocumentRequirementsQuery : IRequest<Response<List<DocumentRequirementDto>>>
    {
        public RequestType RequestType { get; set; } = RequestType.Connection;
        /// <summary>Capacidad requerida; por encima de 10 kVA la conexión exige proyecto de ingeniería.</summary>
        public decimal RequestedCapacityKva { get; set; }
        /// <summary>Empresa o institución (persona jurídica).</summary>
        public bool IsLegalEntity { get; set; }
        public PropertyRelation PropertyRelation { get; set; } = PropertyRelation.Owner;
        public UsageType UsageType { get; set; } = UsageType.Residential;
    }

    public class GetDocumentRequirementsValidator : AbstractValidator<GetDocumentRequirementsQuery>
    {
        public GetDocumentRequirementsValidator()
        {
            RuleFor(q => q.RequestType).IsInEnum().WithMessage("Tipo de solicitud inválido.");
            RuleFor(q => q.RequestedCapacityKva).InclusiveBetween(0m, 100_000m).WithMessage("La capacidad debe estar entre 0 y 100.000 kVA.");
            RuleFor(q => q.PropertyRelation).IsInEnum().WithMessage("Relación con el inmueble inválida.");
            RuleFor(q => q.UsageType).IsInEnum().WithMessage("Tipo de uso inválido.");
        }
    }

    public class GetDocumentRequirementsHandler : IRequestHandler<GetDocumentRequirementsQuery, Response<List<DocumentRequirementDto>>>
    {
        public Task<Response<List<DocumentRequirementDto>>> Handle(GetDocumentRequirementsQuery request, CancellationToken cancellationToken)
        {
            // Solicitud hipotética: solo con los datos que deciden los requisitos.
            var hypothetical = new ConnectionRequest
            {
                RequestType = request.RequestType,
                RequestedCapacityKva = request.RequestedCapacityKva,
                PropertyRelation = request.PropertyRelation,
                UsageType = request.UsageType
            };

            var items = RequiredDocuments.For(hypothetical, request.IsLegalEntity)
                .Select(r => new DocumentRequirementDto
                {
                    Code = r.Code,
                    Description = r.Description,
                    AcceptedTypes = r.AnyOf.Select(t => t.ToString()).ToList()
                })
                .ToList();

            return Task.FromResult(new Response<List<DocumentRequirementDto>>(items));
        }
    }
}
