using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Catalogs
{
    // Catálogos públicos para los filtros del mapa (B-1, RF-03).

    public class ServiceZoneDto
    {
        public int Id { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string? Municipality { get; set; }
        public string? Province { get; set; }
    }

    public class DistributorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public record ListServiceZonesQuery : IRequest<Response<List<ServiceZoneDto>>>;

    public class ListServiceZonesHandler(IGenericRepository<ServiceZone> zones)
        : IRequestHandler<ListServiceZonesQuery, Response<List<ServiceZoneDto>>>
    {
        public async Task<Response<List<ServiceZoneDto>>> Handle(ListServiceZonesQuery request, CancellationToken cancellationToken)
        {
            var all = await zones.GetAllAsync();
            var items = all
                .OrderBy(z => z.Province)
                .ThenBy(z => z.ZoneName, StringComparer.CurrentCulture)
                .Select(z => new ServiceZoneDto { Id = z.Id, ZoneName = z.ZoneName, Municipality = z.Municipality, Province = z.Province })
                .ToList();
            return new Response<List<ServiceZoneDto>>(items);
        }
    }

    public record ListDistributorsQuery : IRequest<Response<List<DistributorDto>>>;

    public class ListDistributorsHandler(IGenericRepository<Distributor> distributors)
        : IRequestHandler<ListDistributorsQuery, Response<List<DistributorDto>>>
    {
        public async Task<Response<List<DistributorDto>>> Handle(ListDistributorsQuery request, CancellationToken cancellationToken)
        {
            var all = await distributors.GetAllAsync();
            // Las desactivadas no se ofrecen en el mapa ni en los formularios.
            var items = all
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name, StringComparer.CurrentCulture)
                .Select(d => new DistributorDto { Id = d.Id, Name = d.Name, Type = d.Type.ToString() })
                .ToList();
            return new Response<List<DistributorDto>>(items);
        }
    }
}
