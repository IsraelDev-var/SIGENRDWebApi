using SIGENRD.Core.Application.Wrappers;
using Asp.Versioning;
using SIGENRD.Core.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Notifications;

namespace SIGENRDWebApi.Controllers.v1.Notifications
{
    /// <summary>Notificaciones internas del usuario autenticado, sea cual sea su rol (RF-32).</summary>
    [ApiVersion("1.0")]
    [Authorize]
    public class NotificationsController(IMediator mediator) : BaseApiController
    {
        [ProducesResponseType<Response<PagedDto<NotificationDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ListMyNotificationsQuery query)
            => Ok(await mediator.Send(query));

        [ProducesResponseType<Response<int>>(StatusCodes.Status200OK)]
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
            => Ok(await mediator.Send(new GetUnreadNotificationCountQuery()));

        [ProducesResponseType<Response<NotificationDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
            => Ok(await mediator.Send(new MarkNotificationReadCommand(id)));

        [ProducesResponseType<Response<int>>(StatusCodes.Status200OK)]
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await mediator.Send(new MarkAllNotificationsReadCommand()));
    }
}
