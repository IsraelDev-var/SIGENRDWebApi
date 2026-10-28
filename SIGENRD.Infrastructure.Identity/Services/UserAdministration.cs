using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Infrastructure.Identity.Entities;

namespace SIGENRD.Infrastructure.Identity.Services
{
    /// <summary>Usuario interno tal como lo ve el administrador (B-5).</summary>
    public class InternalUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        /// <summary>EDE del ingeniero o la distribuidora; null para el administrador.</summary>
        public int? DistributorId { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        /// <summary>Bloqueada por intentos fallidos (se levanta sola o al restablecer la contraseña).</summary>
        public bool IsLockedOut { get; set; }
    }

    public interface IUserAdministration
    {
        Task<PagedDto<InternalUserDto>> ListInternalAsync(string? text, UserType? role, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<InternalUserDto> SetActiveAsync(string userId, bool active, CancellationToken cancellationToken = default);

        /// <summary>Cambia la EDE de un ingeniero o una distribuidora. Su token vigente deja de servir (lleva la EDE anterior).</summary>
        Task<InternalUserDto> SetDistributorAsync(string userId, int distributorId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Usuarios internos (ingeniero, distribuidora, administrador): listar, desactivar y reactivar (B-5, matriz 3.2).
    /// Los clientes y las instaladoras no se gestionan aquí: se registran solos.
    /// </summary>
    public class UserAdministration(
        UserManager<ApplicationUser> userManager, ICurrentUserService currentUser, ISecurityAuditLog securityAudit, AccountSessions accountSessions)
        : IUserAdministration
    {
        public static readonly UserType[] InternalTypes = { UserType.Engineer, UserType.Distributor, UserType.Admin };

        private IQueryable<ApplicationUser> Internal => userManager.Users.Where(u => InternalTypes.Contains(u.UserType));

        public async Task<PagedDto<InternalUserDto>> ListInternalAsync(
            string? text, UserType? role, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) throw new ApiException("La página debe ser 1 o mayor.");
            if (pageSize is < 1 or > PagedDto<InternalUserDto>.MaxPageSize)
                throw new ApiException("El tamaño de página debe estar entre 1 y " + PagedDto<InternalUserDto>.MaxPageSize + ".");
            if (text is { Length: > 100 }) throw new ApiException("El texto de búsqueda no puede exceder los 100 caracteres.");
            if (role.HasValue && !InternalTypes.Contains(role.Value)) throw new ApiException("Rol inválido: solo ingeniero, distribuidora o administrador.");

            var query = Internal;
            if (role.HasValue) query = query.Where(u => u.UserType == role);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var term = text.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);
            var users = await query
                .OrderBy(u => u.DeactivatedAt != null).ThenBy(u => u.FullName).ThenBy(u => u.Email)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedDto<InternalUserDto>
            {
                Items = users.Select(ToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<InternalUserDto> SetActiveAsync(string userId, bool active, CancellationToken cancellationToken = default)
        {
            var user = await Internal.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuario interno no encontrado.");

            if (!active)
            {
                if (user.Id == currentUser.UserId)
                    throw new ApiException("No puedes desactivar tu propia cuenta.");

                // Siempre debe quedar alguien que pueda administrar usuarios.
                if (user.UserType == UserType.Admin
                    && !await Internal.AnyAsync(u => u.UserType == UserType.Admin && u.DeactivatedAt == null && u.Id != user.Id, cancellationToken))
                    throw new ApiException("No se puede desactivar al último administrador activo.");
            }

            var changed = active ? user.DeactivatedAt is not null : user.DeactivatedAt is null;
            if (changed)
            {
                user.DeactivatedAt = active ? null : DateTime.UtcNow;
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new ApiException(string.Join(" ", result.Errors.Select(e => e.Description)));

                // Cambia el sello de seguridad: invalida lo que dependa de él (enlaces de restablecimiento pendientes).
                await userManager.UpdateSecurityStampAsync(user);
                accountSessions.Forget(user.Id);
                await securityAudit.RecordAsync(
                    active ? SecurityEvents.AccountReactivated : SecurityEvents.AccountDeactivated, user.Id, user.Email ?? string.Empty, cancellationToken);
            }

            return ToDto(user);
        }

        public async Task<InternalUserDto> SetDistributorAsync(string userId, int distributorId, CancellationToken cancellationToken = default)
        {
            var user = await Internal.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuario interno no encontrado.");

            if (!StaffTypes.Contains(user.UserType))
                throw new ApiException("El administrador no pertenece a ninguna distribuidora.");

            if (user.DistributorId != distributorId)
            {
                user.DistributorId = distributorId;
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new ApiException(string.Join(" ", result.Errors.Select(e => e.Description)));

                // El token vigente lleva la EDE anterior: cambiar el sello lo invalida (A28-08).
                await userManager.UpdateSecurityStampAsync(user);
                accountSessions.Forget(user.Id);
            }

            return ToDto(user);
        }

        /// <summary>Roles internos que pertenecen a una EDE.</summary>
        public static readonly UserType[] StaffTypes = { UserType.Engineer, UserType.Distributor };

        private static InternalUserDto ToDto(ApplicationUser u) => new()
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            Role = u.UserType.ToString(),
            DistributorId = u.DistributorId,
            EmailConfirmed = u.EmailConfirmed,
            IsActive = u.DeactivatedAt is null,
            DeactivatedAt = u.DeactivatedAt,
            IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
        };
    }
}
