using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Features.Distributors;
using SIGENRD.Core.Application.Mediator;

namespace SIGENRDWebApi.Services
{
    /// <summary>
    /// La EDE que se asigna a un ingeniero o una distribuidora tiene que existir y estar activa. Lo comprueban los
    /// controladores porque Identity no conoce las distribuidoras (viven en la base de negocio).
    /// </summary>
    public static class StaffDistributors
    {
        public static async Task EnsureAssignableAsync(IMediator mediator, int distributorId, CancellationToken cancellationToken)
        {
            DistributorDetailDto? distributor = null;
            try
            {
                distributor = (await mediator.Send(new GetDistributorQuery(distributorId), cancellationToken)).Data;
            }
            catch (KeyNotFoundException)
            {
            }

            if (distributor is null || !distributor.IsActive)
                throw new ApiException("La distribuidora indicada no existe o está desactivada.");
        }
    }
}
