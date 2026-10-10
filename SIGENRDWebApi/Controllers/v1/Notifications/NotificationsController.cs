using Asp.Versioning;
using MediatR;
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
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ListMyNotificationsQuery query)
            => Ok(await mediator.Send(query));

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
            => Ok(await mediator.Send(new GetUnreadNotificationCountQuery()));

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
            => Ok(await mediator.Send(new MarkNotificationReadCommand(id)));

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await mediator.Send(new MarkAllNotificationsReadCommand()));
    }
}
