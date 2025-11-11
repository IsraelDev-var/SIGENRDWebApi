

using Microsoft.AspNetCore.Identity;

namespace SIGENRD.Infrastruture.Identity.Entities
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
