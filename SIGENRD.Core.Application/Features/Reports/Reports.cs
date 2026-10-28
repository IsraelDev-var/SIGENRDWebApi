using FluentValidation;
using SIGENRD.Core.Application.Features.Documents;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Reports
{
    // Panel y reportes de la distribuidora (RF-27, RF-28, RF-30). Las zonas críticas (RF-29) están en Transformers.

    // ---------------- filtro común ----------------

    /// <summary>Período por fecha de presentación: [From, To). Por defecto, los últimos 12 meses.</summary>
    public abstract class ReportPeriodQuery
    {
        public const int MaxDays = 3 * 366;

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        /// <summary>Solo lo usa el administrador; el personal de una EDE siempre ve la suya.</summary>
        public int? DistributorId { get; set; }

        public (DateTime From, DateTime To) Period()
        {
            var to = Utc(To) ?? DateTime.UtcNow.Date.AddDays(1);
            var from = Utc(From) ?? to.AddMonths(-12);
            return (from, to);
        }

        /// <summary>Una fecha sin zona (la de la URL) se toma como UTC, no como la hora local del servidor.</summary>
        private static DateTime? Utc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } v => v,
            { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
            var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
        };
    }

    public abstract class ReportPeriodValidator<T> : AbstractValidator<T> where T : ReportPeriodQuery
    {
        protected ReportPeriodValidator()
        {
            RuleFor(q => q).Must(q => { var (from, to) = q.Period(); return from < to; })
                .WithMessage("La fecha inicial debe ser anterior a la final.");
            RuleFor(q => q).Must(q => { var (from, to) = q.Period(); return (to - from).TotalDays <= ReportPeriodQuery.MaxDays; })
                .WithMessage("El período no puede superar los 3 años.");
        }
    }

    // ---------------- DTOs ----------------

    public record CountItem(string Key, int Count);

    public record MonthItem(string Month, int Total, int Approved, int Rejected);

    public class RequestStatisticsDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Total { get; set; }

        /// <summary>Recibidas, en revisión, con observaciones o en corrección: lo que aún espera.</summary>
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }

        /// <summary>Aprobadas sobre resueltas (aprobadas + rechazadas), en %. null sin resueltas.</summary>
        public decimal? ApprovalRatePct { get; set; }

        public decimal RequestedKva { get; set; }
        public decimal ApprovedKva { get; set; }

        /// <summary>Tiempos de revisión, en días (RF-27).</summary>
        public decimal? AvgDaysToFirstReview { get; set; }
        public decimal? AvgDaysToResolution { get; set; }
        public decimal? MedianDaysToResolution { get; set; }

        public List<CountItem> ByStatus { get; set; } = new();
        public List<CountItem> ByType { get; set; } = new();
        public List<CountItem> ByZone { get; set; } = new();
        public List<CountItem> ByDistributor { get; set; } = new();
        public List<MonthItem> ByMonth { get; set; } = new();
    }

    public class InstallerRankingItem
    {
        public int Rank { get; set; }
        public int InstallerCompanyId { get; set; }
        public string TradeName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public decimal? ApprovalRatePct { get; set; }
        public decimal RequestedKva { get; set; }
    }

    /// <summary>Todo lo que lleva un reporte exportado.</summary>
    public record RequestReport(
        RequestStatisticsDto Statistics,
        IReadOnlyList<InstallerRankingItem> Ranking,
        IReadOnlyList<RequestReportRow> Rows,
        DateTime GeneratedAt);

    public enum ReportFormat { Xlsx = 1, Pdf = 2 }

    public interface IReportExporter
    {
        ReportFormat Format { get; }
        string ContentType { get; }
        string Extension { get; }
        byte[] Export(RequestReport report);
    }

    // ---------------- cálculo ----------------

    /// <summary>Agregaciones de los reportes. Funciones puras sobre las filas: fáciles de probar.</summary>
    public static class ReportCalculator
    {
        private static readonly RequestStatus[] PendingStatuses =
            { RequestStatus.Received, RequestStatus.UnderReview, RequestStatus.Observed, RequestStatus.InCorrection };

        public static RequestStatisticsDto Statistics(IReadOnlyList<RequestReportRow> rows, DateTime from, DateTime to)
        {
            var approved = rows.Count(r => r.Status == RequestStatus.Approved);
            var rejected = rows.Count(r => r.Status == RequestStatus.Rejected);

            var toReview = rows.Where(r => r.FirstReviewAt.HasValue).Select(r => Days(r.RequestedAt, r.FirstReviewAt!.Value)).ToList();
            var toResolution = rows.Where(r => r.ResolvedAt.HasValue).Select(r => Days(r.RequestedAt, r.ResolvedAt!.Value)).OrderBy(d => d).ToList();

            return new RequestStatisticsDto
            {
                From = from,
                To = to,
                Total = rows.Count,
                Pending = rows.Count(r => PendingStatuses.Contains(r.Status)),
                Approved = approved,
                Rejected = rejected,
                Cancelled = rows.Count(r => r.Status == RequestStatus.Cancelled),
                ApprovalRatePct = Rate(approved, approved + rejected),
                RequestedKva = rows.Sum(r => r.RequestedCapacityKva),
                ApprovedKva = rows.Where(r => r.Status == RequestStatus.Approved).Sum(r => r.RequestedCapacityKva),
                AvgDaysToFirstReview = toReview.Count == 0 ? null : Math.Round(toReview.Average(), 1),
                AvgDaysToResolution = toResolution.Count == 0 ? null : Math.Round(toResolution.Average(), 1),
                MedianDaysToResolution = toResolution.Count == 0 ? null : Math.Round(Median(toResolution), 1),
                ByStatus = Count(rows, r => r.Status.ToString()),
                ByType = Count(rows, r => r.RequestType.ToString()),
                ByZone = Count(rows, r => r.ZoneName),
                ByDistributor = Count(rows, r => r.DistributorName),
                ByMonth = rows
                    .GroupBy(r => r.RequestedAt.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .Select(g => new MonthItem(g.Key, g.Count(),
                        g.Count(r => r.Status == RequestStatus.Approved), g.Count(r => r.Status == RequestStatus.Rejected)))
                    .ToList()
            };
        }

        /// <summary>Instaladoras más activas (RF-28): más solicitudes primero; a igualdad, más aprobadas.</summary>
        public static List<InstallerRankingItem> Ranking(IReadOnlyList<RequestReportRow> rows, int top)
        {
            var ranking = rows
                .Where(r => r.InstallerCompanyId.HasValue)
                .GroupBy(r => (Id: r.InstallerCompanyId!.Value, Name: r.InstallerName ?? string.Empty))
                .Select(g =>
                {
                    var approved = g.Count(r => r.Status == RequestStatus.Approved);
                    var rejected = g.Count(r => r.Status == RequestStatus.Rejected);
                    return new InstallerRankingItem
                    {
                        InstallerCompanyId = g.Key.Id,
                        TradeName = g.Key.Name,
                        Total = g.Count(),
                        Approved = approved,
                        Rejected = rejected,
                        ApprovalRatePct = Rate(approved, approved + rejected),
                        RequestedKva = g.Sum(r => r.RequestedCapacityKva)
                    };
                })
                .OrderByDescending(i => i.Total).ThenByDescending(i => i.Approved).ThenBy(i => i.TradeName)
                .Take(top)
                .ToList();

            for (var i = 0; i < ranking.Count; i++) ranking[i].Rank = i + 1;
            return ranking;
        }

        private static List<CountItem> Count(IEnumerable<RequestReportRow> rows, Func<RequestReportRow, string> key) =>
            rows.GroupBy(key).Select(g => new CountItem(g.Key, g.Count()))
                .OrderByDescending(c => c.Count).ThenBy(c => c.Key).ToList();

        private static decimal Days(DateTime from, DateTime to) => (decimal)(to - from).TotalDays;

        private static decimal? Rate(int part, int whole) => whole == 0 ? null : Math.Round(part * 100m / whole, 1);

        private static decimal Median(IReadOnlyList<decimal> sorted) =>
            sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2m;
    }

    // ---------------- estadísticas (RF-27) ----------------

    public class GetRequestStatisticsQuery : ReportPeriodQuery, IRequest<Response<RequestStatisticsDto>> { }

    public class GetRequestStatisticsValidator : ReportPeriodValidator<GetRequestStatisticsQuery> { }

    public class GetRequestStatisticsHandler(IReportRepository reports, ICurrentUserService currentUser)
        : IRequestHandler<GetRequestStatisticsQuery, Response<RequestStatisticsDto>>
    {
        public async Task<Response<RequestStatisticsDto>> Handle(GetRequestStatisticsQuery query, CancellationToken cancellationToken)
        {
            var (from, to) = query.Period();
            var rows = await reports.GetRequestRowsAsync(from, to, DistributorScope.Filter(currentUser, query.DistributorId), cancellationToken);
            return new Response<RequestStatisticsDto>(ReportCalculator.Statistics(rows, from, to));
        }
    }

    // ---------------- ranking de instaladoras (RF-28) ----------------

    public class GetInstallerRankingQuery : ReportPeriodQuery, IRequest<Response<List<InstallerRankingItem>>>
    {
        public int Top { get; set; } = 10;
    }

    public class GetInstallerRankingValidator : ReportPeriodValidator<GetInstallerRankingQuery>
    {
        public GetInstallerRankingValidator()
        {
            RuleFor(q => q.Top).InclusiveBetween(1, 100).WithMessage("Top debe estar entre 1 y 100.");
        }
    }

    public class GetInstallerRankingHandler(IReportRepository reports, ICurrentUserService currentUser)
        : IRequestHandler<GetInstallerRankingQuery, Response<List<InstallerRankingItem>>>
    {
        public async Task<Response<List<InstallerRankingItem>>> Handle(GetInstallerRankingQuery query, CancellationToken cancellationToken)
        {
            var (from, to) = query.Period();
            var rows = await reports.GetRequestRowsAsync(from, to, DistributorScope.Filter(currentUser, query.DistributorId), cancellationToken);
            return new Response<List<InstallerRankingItem>>(ReportCalculator.Ranking(rows, query.Top));
        }
    }

    // ---------------- exportar (RF-30) ----------------

    public class ExportRequestReportQuery : ReportPeriodQuery, IRequest<DocumentFile>
    {
        public ReportFormat Format { get; set; } = ReportFormat.Xlsx;
    }

    public class ExportRequestReportValidator : ReportPeriodValidator<ExportRequestReportQuery>
    {
        public ExportRequestReportValidator()
        {
            RuleFor(q => q.Format).IsInEnum().WithMessage("Formato no válido: xlsx o pdf.");
        }
    }

    public class ExportRequestReportHandler(IReportRepository reports, IEnumerable<IReportExporter> exporters, ICurrentUserService currentUser)
        : IRequestHandler<ExportRequestReportQuery, DocumentFile>
    {
        public async Task<DocumentFile> Handle(ExportRequestReportQuery query, CancellationToken cancellationToken)
        {
            var (from, to) = query.Period();
            var rows = await reports.GetRequestRowsAsync(from, to, DistributorScope.Filter(currentUser, query.DistributorId), cancellationToken);
            var report = new RequestReport(ReportCalculator.Statistics(rows, from, to), ReportCalculator.Ranking(rows, 20), rows, DateTime.UtcNow);

            var exporter = exporters.Single(e => e.Format == query.Format);
            var bytes = exporter.Export(report);
            var name = $"reporte-solicitudes-{from:yyyyMMdd}-{to.AddDays(-1):yyyyMMdd}{exporter.Extension}";

            return new DocumentFile(name, exporter.ContentType, new MemoryStream(bytes, writable: false));
        }
    }
}
