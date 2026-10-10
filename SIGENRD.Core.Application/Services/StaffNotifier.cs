using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// Deja una notificación interna a quienes revisan y deciden (Distributor y Admin) cuando llega algo nuevo.
    /// Solo agrega las filas: guarda quien la llama, dentro de su misma transacción.
    /// </summary>
    public class StaffNotifier(IStaffDirectory staff, IUnitOfWork unitOfWork)
    {
        private static readonly string[] Recipients = { nameof(UserType.Distributor), nameof(UserType.Admin) };

        public async Task NotifyAsync(string title, string message, NotificationType type, CancellationToken cancellationToken = default)
        {
            var userIds = await staff.GetUserIdsInRolesAsync(Recipients, cancellationToken);

            foreach (var userId in userIds)
            {
                await unitOfWork.Repository<Notification>().AddAsync(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    NotificationType = type
                });
            }
        }
    }
}
