using AutoMapper;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;
using SIGENRD.Core.Domain.Entities;


namespace SIGENRD.Core.Application.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            // 🔄 CAMBIO: Ahora mapeamos desde el COMMAND hacia la Entidad
            CreateMap<CreateConnectionRequestCommand, ConnectionRequest>()
                .ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src =>
                    new Point(src.Longitude, src.Latitude) { SRID = 4326 }))
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RequestCode, opt => opt.Ignore());

            // ✅ ESTE SE QUEDA IGUAL (Entidad -> ResponseDto)
            CreateMap<ConnectionRequest, ConnectionRequestResponseDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinates != null ? src.Coordinates.Y : 0))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinates != null ? src.Coordinates.X : 0));
        }
    }
}
