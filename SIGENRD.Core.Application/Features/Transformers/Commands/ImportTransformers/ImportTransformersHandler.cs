using SIGENRD.Core.Application.Mediator;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers
{
    public class ImportTransformersHandler(IUnitOfWork unitOfWork, ITransformerRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<ImportTransformersCommand, Response<ImportTransformersResultDto>>
    {
        public async Task<Response<ImportTransformersResultDto>> Handle(ImportTransformersCommand request, CancellationToken cancellationToken)
        {
            // El personal de una EDE solo importa en la suya, indique lo que indique.
            var distributor = await unitOfWork.Repository<Distributor>().GetByIdAsync(DistributorScope.ForWrite(currentUser, request.DistributorId));
            if (distributor is null || !distributor.IsActive)
                throw new ApiException("La distribuidora indicada no existe o está desactivada.");

            var parsed = await TransformerCsvParser.ParseAsync(request.Content, cancellationToken);
            var errors = new List<string>(parsed.Errors);

            if (errors.Count == 0)
            {
                var zones = (await unitOfWork.Repository<ServiceZone>().GetAllAsync()).Select(z => z.Id).ToHashSet();
                var existing = await repository.GetExistingCodesAsync(parsed.Rows.Select(r => r.Code), cancellationToken);

                foreach (var row in parsed.Rows)
                {
                    if (!zones.Contains(row.ServiceZoneId))
                        errors.Add("Línea " + row.Line + ": la zona " + row.ServiceZoneId + " no existe.");

                    if (existing.Contains(row.Code))
                        errors.Add("Línea " + row.Line + ": ya existe un transformador con el código " + row.Code + ".");
                }
            }

            if (errors.Count > 0)
            {
                var failure = new ValidationException();
                failure.Errors.AddRange(errors);
                throw failure;
            }

            var transformers = parsed.Rows.Select(row =>
            {
                var transformer = new Transformer
                {
                    Code = row.Code,
                    DistributorId = distributor.Id,
                    ServiceZoneId = row.ServiceZoneId,
                    Location = new Point(row.Longitude, row.Latitude) { SRID = 4326 }
                };

                transformer.UpdateCapacity(
                    row.TotalCapacityKva, row.AvailableCapacityKva,
                    distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);

                return transformer;
            }).ToList();

            await repository.AddRangeAsync(transformers, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            return new Response<ImportTransformersResultDto>(
                new ImportTransformersResultDto { Imported = transformers.Count },
                transformers.Count + " transformadores importados.");
        }
    }
}
