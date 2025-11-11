using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Infrastructure.Identity.Entities;


namespace SIGENRD.Infrastructure.Identity.Configurations
{
    public class IdentityDbContextSIGENRD(DbContextOptions<IdentityDbContextSIGENRD> options)
                : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
