using Microsoft.AspNetCore.Identity;


namespace SIGENRD.Infrastruture.Identity.Entities
{
    public class ApplicationRole : IdentityUser
    {
        public required string RoleName { get; set; } = string.Empty;
    }
}
