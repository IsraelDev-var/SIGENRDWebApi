using Microsoft.AspNetCore.Identity;

namespace SIGENRD.Infrastructure.Identity.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
    }
}
