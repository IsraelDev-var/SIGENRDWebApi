using MediatR;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;


namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    // Este comando actúa como tu antiguo DTO de entrada, pero implementa IRequest
    public class CreateConnectionRequestCommand : IRequest<Response<int>>
    {
        public int CustomerId { get; set; }
        public int InstallerCompanyId { get; set; }
        public int DistributorId { get; set; }
        public int TransformerId { get; set; }
        public UsageType UsageType { get; set; }
        public TariffType TariffType { get; set; }
        public InterconnectionType InterconnectionType { get; set; }
        public string? ProjectDescription { get; set; }
        public string ProjectAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
