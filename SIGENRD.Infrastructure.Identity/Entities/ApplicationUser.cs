

using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Infrastructure.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }

        public UserType UserType { get; set; }  // Customer / Engineer / Company / Distributor
        public DistributorType? DistributorType { get; set; }

        public int? InstallerCompanyId { get; set; }

    }
}
