using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer
{
    public class CreateTransformerCommand : IRequest<Response<int>>
    {
        public string Code { get; set; } = string.Empty;
        public int DistributorId { get; set; }
        public int ServiceZoneId { get; set; }

        // Capacidades en kVA (RN-01)
        public decimal TotalCapacityKva { get; set; }
        public decimal AvailableCapacityKva { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
