using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using System.Reflection;

namespace SIGENRD.Infrastructure.Persistences.Contexts
{
    public class AppContextSIGENRD(DbContextOptions<AppContextSIGENRD> options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
        public DbSet<Distributor> Distributors { get; set; }
        public DbSet<InstallerCompany> InstallerCompanies { get; set; }
        public DbSet<Transformer> Transformers { get; set; }

        public DbSet<ServiceZone> ServiceZones { get; set; }

        public DbSet<TechnicalDocument> TechnicalDocuments { get; set; }
        public DbSet<StateHistory> StateHistories { get; set; }
        public DbSet<ReviewObservation> ReviewObservations { get; set; }
        public DbSet<NetMeteringRequest> NetMeteringRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        }

    }
}
