using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// EDE sobre la que actúa el usuario actual en la gestión de la red y en los reportes.
    /// El administrador elige cualquiera; el personal de una EDE (Ingeniero o Distribuidora) solo la suya.
    /// </summary>
    public static class DistributorScope
    {
        public const string NoDistributorMessage =
            "Tu cuenta no tiene una distribuidora asignada. Pide al administrador que te asigne una.";

        public static bool IsAdmin(ICurrentUserService user) => user.IsInRole(nameof(UserType.Admin));

        /// <summary>Filtro de lectura: lo pedido si es el administrador (null = todas), la EDE propia si es personal.</summary>
        public static int? Filter(ICurrentUserService user, int? requested = null) =>
            IsAdmin(user) ? requested : Own(user);

        /// <summary>EDE para un alta (transformador, importación): la indicada si es el administrador, la propia si no.</summary>
        public static int ForWrite(ICurrentUserService user, int requested) =>
            IsAdmin(user) ? requested : Own(user);

        /// <summary>Un registro de otra EDE no existe para el personal: 404, igual que una solicitud ajena (RN-22).</summary>
        public static void EnsureCanManage(ICurrentUserService user, int distributorId, string notFoundMessage)
        {
            if (!IsAdmin(user) && Own(user) != distributorId)
                throw new KeyNotFoundException(notFoundMessage);
        }

        private static int Own(ICurrentUserService user) =>
            user.DistributorId ?? throw new ForbiddenException(NoDistributorMessage);
    }
}
