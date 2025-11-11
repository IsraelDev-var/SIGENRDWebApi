
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Infrastruture.Identity.Entities;

namespace SIGENRD.Infrastruture.Identity.Configurations
{
    public class IdentityDbContextSIGENRD : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public IdentityDbContextSIGENRD(DbContextOptions<IdentityDbContextSIGENRD> options)
           : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
