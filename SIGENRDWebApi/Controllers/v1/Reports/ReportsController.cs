using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Reports;
using SIGENRD.Core.Application.Mediator;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Reports
{
    /// <summary>
    /// Panel y reportes (RF-27, RF-28, RF-30) para la distribuidora y la administración.
    /// Período por fecha de presentación, ?from=2026-01-01&amp;to=2026-07-01 (to no incluido); por defecto, 12 meses.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
    public class ReportsController(IMediator mediator) : BaseApiController
    {
        /// <summary>Totales, tasas, tiempos de revisión y desgloses por estado, tipo, zona, distribuidora y mes.</summary>
        [HttpGet("requests/statistics")]
        public async Task<IActionResult> Statistics([FromQuery] GetRequestStatisticsQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Instaladoras más activas del período.</summary>
        [HttpGet("installers/ranking")]
        public async Task<IActionResult> InstallerRanking([FromQuery] GetInstallerRankingQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Reporte descargable: format=Xlsx (con el detalle de cada solicitud) o format=Pdf.</summary>
        [HttpGet("requests/export")]
        public async Task<IActionResult> Export([FromQuery] ExportRequestReportQuery query, CancellationToken cancellationToken)
        {
            var file = await mediator.Send(query, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
