using System.Text.Json;
using FluentValidation;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Features.Documents;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules.Simulation;

namespace SIGENRD.Core.Application.Features.Simulations
{
    // Simulación de medición neta (RF-18 a RF-26, CA-10). Informativa: no reserva capacidad ni es una solicitud.

    // ---------------- DTOs ----------------

    public class SimulationEquipmentDto
    {
        public string GenerationSystemType { get; set; } = string.Empty;
        public string? PanelModel { get; set; }
        public string? InverterModel { get; set; }
        public decimal RatedPowerKw { get; set; }
        public int Quantity { get; set; }
    }

    public class NearbyTransformerDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public double DistanceMeters { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal EffectiveAvailableKva { get; set; }
    }

    public class SimulationSummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal MonthlyConsumptionKwh { get; set; }
        public decimal InstalledCapacityKw { get; set; }
        public decimal AnnualSavings { get; set; }
        public decimal? PaybackYears { get; set; }
        public decimal Roi { get; set; }
        public string ViabilityStatus { get; set; } = string.Empty;
    }

    public class SimulationDto : SimulationSummaryDto
    {
        // Parámetros
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? FlatPricePerKwh { get; set; }
        public decimal InvestmentCost { get; set; }
        public List<SimulationEquipmentDto> Equipment { get; set; } = new();

        // Resultados
        public decimal AnnualProductionKwh { get; set; }
        public decimal AnnualConsumptionKwh { get; set; }
        public decimal CoveragePct { get; set; }
        public decimal AnnualBillBefore { get; set; }
        public decimal AnnualBillAfter { get; set; }
        public decimal AnnualExportCreditKwh { get; set; }
        public decimal SurplusPayment { get; set; }
        public List<string> Recommendations { get; set; } = new();

        /// <summary>Transformador de referencia guardado con la simulación.</summary>
        public NearbyTransformerDto? ReferenceTransformer { get; set; }

        /// <summary>Solo al crearla: los transformadores encontrados en el radio de búsqueda (RF-23).</summary>
        public List<NearbyTransformerDto>? NearbyTransformers { get; set; }

        public string FormulaVersion { get; set; } = string.Empty;
        public JsonElement Assumptions { get; set; }

        public string Disclaimer { get; set; } = SimulationMapper.Disclaimer;
    }

    public class SimulationComparisonDto
    {
        public List<SimulationDto> Simulations { get; set; } = new();
        public int? BestPaybackId { get; set; }
        public int? BestRoiId { get; set; }
        public int? HighestSavingsId { get; set; }
    }

    internal static class SimulationMapper
    {
        public const string Disclaimer =
            "Simulación informativa, sin carácter regulatorio ni vinculante. No reserva capacidad del transformador. " +
            "Los resultados dependen de los supuestos indicados y de la tarifa vigente.";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static string SerializeAssumptions(SimulationAssumptions a, decimal powerFactor) =>
            JsonSerializer.Serialize(new
            {
                a.SpecificYieldKwhPerKwp,
                a.AnnualDegradationPct,
                a.LifetimeYears,
                a.CostPerKwp,
                a.SurplusPaymentPct,
                a.ProfitablePaybackYears,
                a.ConditionalPaybackYears,
                PowerFactor = powerFactor,
                Tariff = new
                {
                    a.Tariff.Source,
                    a.Tariff.AllEnergyAtLastBlockFromKwh,
                    Blocks = a.Tariff.Blocks.Select(b => new { b.UpToKwh, b.PricePerKwh })
                }
            }, Json);

        public static SimulationSummaryDto ToSummary(NetMeteringSimulation s) => Fill(new SimulationSummaryDto(), s);

        public static SimulationDto ToDto(NetMeteringSimulation s)
        {
            var dto = Fill(new SimulationDto(), s);
            dto.Latitude = s.Location?.Y;
            dto.Longitude = s.Location?.X;
            dto.FlatPricePerKwh = s.FlatPricePerKwh;
            dto.InvestmentCost = s.InvestmentCost;
            dto.Equipment = (s.Generators ?? new List<SimulationGenerator>()).OrderBy(g => g.Id).Select(g => new SimulationEquipmentDto
            {
                GenerationSystemType = g.GenerationSystemType.ToString(),
                PanelModel = g.PanelModel,
                InverterModel = g.InverterModel,
                RatedPowerKw = g.RatedPowerKw,
                Quantity = g.Quantity
            }).ToList();
            dto.AnnualProductionKwh = s.AnnualProductionKwh;
            dto.AnnualConsumptionKwh = s.MonthlyConsumptionKwh * 12m;
            dto.CoveragePct = s.CoveragePct;
            dto.AnnualBillBefore = s.AnnualBillBefore;
            dto.AnnualBillAfter = s.AnnualBillAfter;
            dto.AnnualExportCreditKwh = s.AnnualExportCreditKwh;
            dto.SurplusPayment = s.SurplusPayment;
            dto.Recommendations = s.Recommendations.ToList();
            dto.ReferenceTransformer = s.Transformer is null ? null : new NearbyTransformerDto
            {
                Id = s.Transformer.Id,
                Code = s.Transformer.Code,
                DistanceMeters = Math.Round(s.TransformerDistanceMeters ?? 0),
                Status = s.Transformer.Status.ToString(),
                EffectiveAvailableKva = s.Transformer.EffectiveAvailableKva
            };
            dto.FormulaVersion = s.FormulaVersion;
            using (var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(s.Assumptions) ? "{}" : s.Assumptions))
                dto.Assumptions = doc.RootElement.Clone();
            return dto;
        }

        private static T Fill<T>(T dto, NetMeteringSimulation s) where T : SimulationSummaryDto
        {
            dto.Id = s.Id;
            dto.Name = s.Name;
            dto.CreatedAt = s.CreatedAt;
            dto.MonthlyConsumptionKwh = s.MonthlyConsumptionKwh;
            dto.InstalledCapacityKw = s.InstalledCapacityKw;
            dto.AnnualSavings = s.AnnualSavings;
            dto.PaybackYears = s.PaybackYears;
            dto.Roi = s.Roi;
            dto.ViabilityStatus = s.ViabilityStatus.ToString();
            return dto;
        }
    }

    // ---------------- crear (RF-18 a RF-21, RF-23 a RF-25) ----------------

    public class SimulationEquipmentInput
    {
        public GenerationSystemType GenerationSystemType { get; set; } = GenerationSystemType.Photovoltaic;
        public string? PanelModel { get; set; }
        public string? InverterModel { get; set; }

        /// <summary>Potencia de cada unidad, en kW (kWp para paneles).</summary>
        public decimal RatedPowerKw { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>Fila de paneles: tiene modelo de panel. Si solo tiene modelo de inversor, es un inversor.</summary>
        internal bool IsPanel => !string.IsNullOrWhiteSpace(PanelModel) || string.IsNullOrWhiteSpace(InverterModel);
    }

    public class CreateSimulationCommand : IRequest<Response<SimulationDto>>
    {
        public string? Name { get; set; }
        public decimal MonthlyConsumptionKwh { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        /// <summary>Potencia estimada en kWp. Se puede omitir si se registran los paneles (RF-24).</summary>
        public decimal? InstalledCapacityKw { get; set; }

        /// <summary>Costo de la cotización; si se omite, se estima con el costo por kWp.</summary>
        public decimal? InvestmentCost { get; set; }

        /// <summary>Precio fijo por kWh para tarifas distintas de BTS-1 (comercial, industrial).</summary>
        public decimal? FlatPricePerKwh { get; set; }

        public List<SimulationEquipmentInput> Equipment { get; set; } = new();
    }

    public class CreateSimulationValidator : AbstractValidator<CreateSimulationCommand>
    {
        public CreateSimulationValidator()
        {
            RuleFor(c => c.Name).MaximumLength(100);
            RuleFor(c => c.MonthlyConsumptionKwh).InclusiveBetween(1m, 1_000_000m)
                .WithMessage("El consumo mensual debe estar entre 1 y 1.000.000 kWh.");

            // República Dominicana (con margen): la simulación usa datos solares y tarifas del país.
            RuleFor(c => c.Latitude).InclusiveBetween(17.3, 20.1).WithMessage("La ubicación debe estar en República Dominicana.");
            RuleFor(c => c.Longitude).InclusiveBetween(-72.1, -68.2).WithMessage("La ubicación debe estar en República Dominicana.");

            RuleFor(c => c.InstalledCapacityKw).InclusiveBetween(0.1m, 1000m).When(c => c.InstalledCapacityKw.HasValue)
                .WithMessage("La potencia debe estar entre 0,1 y 1.000 kWp.");
            RuleFor(c => c).Must(c => c.InstalledCapacityKw.HasValue || c.Equipment.Any(e => e.IsPanel))
                .WithMessage("Indique la potencia estimada o registre los paneles.");

            RuleFor(c => c.InvestmentCost).GreaterThan(0).LessThanOrEqualTo(1_000_000_000m).When(c => c.InvestmentCost.HasValue)
                .WithMessage("El costo de inversión debe ser mayor que cero.");
            RuleFor(c => c.FlatPricePerKwh).InclusiveBetween(0.5m, 100m).When(c => c.FlatPricePerKwh.HasValue)
                .WithMessage("El precio por kWh debe estar entre RD$0,50 y RD$100.");

            RuleFor(c => c.Equipment).Must(e => e.Count <= 20).WithMessage("Se admiten hasta 20 equipos.");
            RuleForEach(c => c.Equipment).ChildRules(e =>
            {
                e.RuleFor(x => x.GenerationSystemType).Equal(GenerationSystemType.Photovoltaic)
                    .WithMessage("La simulación es para sistemas fotovoltaicos.");
                e.RuleFor(x => x.RatedPowerKw).InclusiveBetween(0.01m, 1000m).WithMessage("Potencia del equipo fuera de rango.");
                e.RuleFor(x => x.Quantity).InclusiveBetween(1, 10_000).WithMessage("Cantidad de equipos fuera de rango.");
                e.RuleFor(x => x.PanelModel).MaximumLength(150);
                e.RuleFor(x => x.InverterModel).MaximumLength(150);
            });
        }
    }

    public class CreateSimulationHandler(
        ISimulationRepository simulations,
        ITransformerRepository transformers,
        IUnitOfWork unitOfWork,
        INetMeteringFormula formula,
        SimulationSettings settings,
        ICurrentUserService currentUser)
        : IRequestHandler<CreateSimulationCommand, Response<SimulationDto>>
    {
        private const int NearbyLimit = 5;

        public async Task<Response<SimulationDto>> Handle(CreateSimulationCommand command, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            // RF-24: si se registran paneles, su potencia manda sobre la estimada.
            var panelsKw = command.Equipment.Where(e => e.IsPanel).Sum(e => e.RatedPowerKw * e.Quantity);
            var invertersKw = command.Equipment.Where(e => !e.IsPanel).Sum(e => e.RatedPowerKw * e.Quantity);
            var installedKw = panelsKw > 0 ? panelsKw : command.InstalledCapacityKw!.Value;

            // RF-23: transformadores cercanos, solo como referencia.
            var nearby = await transformers.GetNearAsync(command.Latitude, command.Longitude, settings.NearbyRadiusMeters,
                new TransformerFilter(), NearbyLimit, cancellationToken);
            var nearbyDtos = nearby.Select(t => new NearbyTransformerDto
            {
                Id = t.Id,
                Code = t.Code,
                DistanceMeters = Math.Round(Geo.DistanceMeters(command.Latitude, command.Longitude, t.Location.Y, t.Location.X)),
                Status = t.Status.ToString(),
                EffectiveAvailableKva = t.EffectiveAvailableKva
            }).ToList();
            var reference = nearby.FirstOrDefault();

            var powerFactor = settings.DefaultPowerFactor;
            if (reference is not null)
            {
                var distributor = await unitOfWork.Repository<Distributor>().GetByIdAsync(reference.DistributorId);
                if (distributor is { DefaultPowerFactor: > 0 }) powerFactor = distributor.DefaultPowerFactor;
            }

            var assumptions = settings.ToAssumptions();
            var input = new SimulationInput(command.MonthlyConsumptionKwh, installedKw, command.InvestmentCost, command.FlatPricePerKwh);
            var result = formula.Calculate(input, assumptions);

            var recommendations = SimulationRecommendations.For(
                input, result, assumptions,
                reference is null ? null : new NearbyTransformer(reference.Code, nearbyDtos[0].DistanceMeters, reference.Status, reference.EffectiveAvailableKva),
                command.Equipment.Count == 0 ? null : new EquipmentSummary(panelsKw, invertersKw),
                powerFactor);

            var simulation = new NetMeteringSimulation
            {
                UserId = userId,
                Name = string.IsNullOrWhiteSpace(command.Name) ? null : command.Name.Trim(),
                MonthlyConsumptionKwh = command.MonthlyConsumptionKwh,
                Location = Geo.Point(command.Latitude, command.Longitude),
                InstalledCapacityKw = installedKw,
                FlatPricePerKwh = command.FlatPricePerKwh,
                DistributorId = reference?.DistributorId,
                TransformerId = reference?.Id,
                TransformerDistanceMeters = reference is null ? null : nearbyDtos[0].DistanceMeters,
                AnnualProductionKwh = result.AnnualProductionKwh,
                CoveragePct = result.CoveragePct,
                AnnualBillBefore = result.AnnualBillBefore,
                AnnualBillAfter = result.AnnualBillAfter,
                AnnualExportCreditKwh = result.AnnualExportCreditKwh,
                SurplusPayment = result.SurplusPayment,
                AnnualSavings = result.AnnualSavings,
                InvestmentCost = result.InvestmentCost,
                PaybackYears = result.PaybackYears,
                Roi = result.Roi,
                ViabilityStatus = result.Viability,
                Recommendations = recommendations.ToList(),
                FormulaVersion = formula.Version,
                Assumptions = SimulationMapper.SerializeAssumptions(assumptions, powerFactor),
                Generators = command.Equipment.Select(e => new SimulationGenerator
                {
                    GenerationSystemType = e.GenerationSystemType,
                    PanelModel = e.PanelModel?.Trim(),
                    InverterModel = e.InverterModel?.Trim(),
                    RatedPowerKw = e.RatedPowerKw,
                    Quantity = e.Quantity
                }).ToList()
            };

            await simulations.AddAsync(simulation, cancellationToken);

            var dto = SimulationMapper.ToDto(simulation);
            if (reference is not null)
                dto.ReferenceTransformer = nearbyDtos[0];
            dto.NearbyTransformers = nearbyDtos;
            return new Response<SimulationDto>(dto);
        }
    }

    // ---------------- historial (RF-26) ----------------

    public class ListMySimulationsQuery : IRequest<Response<PagedDto<SimulationSummaryDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = PagedDto<SimulationSummaryDto>.DefaultPageSize;
    }

    public class ListMySimulationsValidator : AbstractValidator<ListMySimulationsQuery>
    {
        public ListMySimulationsValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, PagedDto<SimulationSummaryDto>.MaxPageSize);
        }
    }

    public class ListMySimulationsHandler(ISimulationRepository simulations, ICurrentUserService currentUser)
        : IRequestHandler<ListMySimulationsQuery, Response<PagedDto<SimulationSummaryDto>>>
    {
        public async Task<Response<PagedDto<SimulationSummaryDto>>> Handle(ListMySimulationsQuery query, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            var page = await simulations.ListByUserAsync(userId, query.Page, query.PageSize, cancellationToken);

            return new Response<PagedDto<SimulationSummaryDto>>(new PagedDto<SimulationSummaryDto>
            {
                Items = page.Items.Select(SimulationMapper.ToSummary).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            });
        }
    }

    // ---------------- detalle ----------------

    public record GetSimulationQuery(int Id) : IRequest<Response<SimulationDto>>;

    public class GetSimulationHandler(ISimulationRepository simulations, ICurrentUserService currentUser)
        : IRequestHandler<GetSimulationQuery, Response<SimulationDto>>
    {
        public async Task<Response<SimulationDto>> Handle(GetSimulationQuery query, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            // Una simulación ajena responde igual que una inexistente.
            var simulation = await simulations.GetForUserAsync(query.Id, userId, cancellationToken)
                ?? throw new KeyNotFoundException("Simulación " + query.Id + " no encontrada.");

            return new Response<SimulationDto>(SimulationMapper.ToDto(simulation));
        }
    }

    // ---------------- comparar (RF-22) ----------------

    public class CompareSimulationsQuery : IRequest<Response<SimulationComparisonDto>>
    {
        public const int MaxSimulations = 5;

        public List<int> Ids { get; set; } = new();
    }

    public class CompareSimulationsValidator : AbstractValidator<CompareSimulationsQuery>
    {
        public CompareSimulationsValidator()
        {
            RuleFor(q => q.Ids).Must(ids => ids.Distinct().Count() is >= 2 and <= CompareSimulationsQuery.MaxSimulations)
                .WithMessage("Indique entre 2 y " + CompareSimulationsQuery.MaxSimulations + " simulaciones distintas.");
        }
    }

    public class CompareSimulationsHandler(ISimulationRepository simulations, ICurrentUserService currentUser)
        : IRequestHandler<CompareSimulationsQuery, Response<SimulationComparisonDto>>
    {
        public async Task<Response<SimulationComparisonDto>> Handle(CompareSimulationsQuery query, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            var ids = query.Ids.Distinct().ToList();

            var found = await simulations.GetManyForUserAsync(ids, userId, cancellationToken);
            if (found.Count != ids.Count)
                throw new KeyNotFoundException("Alguna de las simulaciones no existe.");

            var dtos = ids.Select(id => SimulationMapper.ToDto(found.Single(s => s.Id == id))).ToList();

            return new Response<SimulationComparisonDto>(new SimulationComparisonDto
            {
                Simulations = dtos,
                BestPaybackId = dtos.Where(d => d.PaybackYears.HasValue).OrderBy(d => d.PaybackYears).FirstOrDefault()?.Id,
                BestRoiId = dtos.OrderByDescending(d => d.Roi).First().Id,
                HighestSavingsId = dtos.OrderByDescending(d => d.AnnualSavings).First().Id
            });
        }
    }

    // ---------------- reporte PDF (CA-10) ----------------

    public interface ISimulationReportGenerator
    {
        byte[] Generate(SimulationDto simulation);
    }

    public record GetSimulationReportQuery(int Id) : IRequest<DocumentFile>;

    public class GetSimulationReportHandler(IMediator mediator, ISimulationReportGenerator reports)
        : IRequestHandler<GetSimulationReportQuery, DocumentFile>
    {
        public async Task<DocumentFile> Handle(GetSimulationReportQuery query, CancellationToken cancellationToken)
        {
            var simulation = (await mediator.Send(new GetSimulationQuery(query.Id), cancellationToken)).Data!;
            var pdf = reports.Generate(simulation);

            return new DocumentFile("Simulacion-" + simulation.Id + ".pdf", "application/pdf", new MemoryStream(pdf, writable: false));
        }
    }
}
