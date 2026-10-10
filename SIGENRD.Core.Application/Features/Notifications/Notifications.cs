using FluentValidation;
using MediatR;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Notifications
{
    // Notificaciones internas del portal (RF-32, RN-17). Cada usuario ve y marca solo las suyas.

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public static NotificationDto From(Notification n) => new()
        {
            Id = n.Id, Title = n.Title, Message = n.Message, Type = n.NotificationType.ToString(),
            IsRead = n.IsRead, CreatedAt = n.CreatedAt
        };
    }

    public class ListMyNotificationsQuery : IRequest<Response<PagedDto<NotificationDto>>>
    {
        public bool UnreadOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = PagedDto<NotificationDto>.DefaultPageSize;
    }

    public record GetUnreadNotificationCountQuery : IRequest<Response<int>>;

    public record MarkNotificationReadCommand(int Id) : IRequest<Response<NotificationDto>>;

    public record MarkAllNotificationsReadCommand : IRequest<Response<int>>;

    public class ListMyNotificationsValidator : AbstractValidator<ListMyNotificationsQuery>
    {
        public ListMyNotificationsValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
            RuleFor(q => q.PageSize).InclusiveBetween(1, PagedDto<NotificationDto>.MaxPageSize)
                .WithMessage("El tamaño de página debe estar entre 1 y " + PagedDto<NotificationDto>.MaxPageSize + ".");
        }
    }

    public class ListMyNotificationsHandler(INotificationRepository notifications, ICurrentUserService currentUser)
        : IRequestHandler<ListMyNotificationsQuery, Response<PagedDto<NotificationDto>>>
    {
        public async Task<Response<PagedDto<NotificationDto>>> Handle(ListMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            var page = await notifications.ListAsync(userId, request.UnreadOnly, request.Page, request.PageSize, cancellationToken);

            return new Response<PagedDto<NotificationDto>>(new PagedDto<NotificationDto>
            {
                Items = page.Items.Select(NotificationDto.From).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            });
        }
    }

    public class GetUnreadNotificationCountHandler(INotificationRepository notifications, ICurrentUserService currentUser)
        : IRequestHandler<GetUnreadNotificationCountQuery, Response<int>>
    {
        public async Task<Response<int>> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            return new Response<int>(await notifications.CountUnreadAsync(userId, cancellationToken));
        }
    }

    public class MarkNotificationReadHandler(INotificationRepository notifications, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        : IRequestHandler<MarkNotificationReadCommand, Response<NotificationDto>>
    {
        public async Task<Response<NotificationDto>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            // Una notificación ajena responde igual que una inexistente.
            var notification = await notifications.GetForUserAsync(request.Id, userId, cancellationToken)
                ?? throw new KeyNotFoundException("Notificación " + request.Id + " no encontrada.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await unitOfWork.SaveChangesAsync();
            }

            return new Response<NotificationDto>(NotificationDto.From(notification));
        }
    }

    public class MarkAllNotificationsReadHandler(INotificationRepository notifications, ICurrentUserService currentUser)
        : IRequestHandler<MarkAllNotificationsReadCommand, Response<int>>
    {
        public async Task<Response<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            var changed = await notifications.MarkAllReadAsync(userId, cancellationToken);

            return new Response<int>(changed, changed + " notificaciones marcadas como leídas.");
        }
    }
}
