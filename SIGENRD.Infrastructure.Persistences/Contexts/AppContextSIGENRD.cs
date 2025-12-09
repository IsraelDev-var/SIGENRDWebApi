using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Entities;
using System.Reflection;

namespace SIGENRD.Infrastructure.Persistences.Contexts
{
    public class AppContextSIGENRD(DbContextOptions<AppContextSIGENRD> options) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<InstallerCompany> InstallerCompanies => Set<InstallerCompany>();
        public DbSet<EngineerUser> EngineerUsers => Set<EngineerUser>();
        public DbSet<Distributor> Distributors => Set<Distributor>();
        public DbSet<ServiceZone> ServiceZones => Set<ServiceZone>();
        public DbSet<Transformer> Transformers => Set<Transformer>();
        public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();
        public DbSet<NetMeteringRequest> NetMeteringRequests => Set<NetMeteringRequest>();
        public DbSet<TechnicalDocument> TechnicalDocuments => Set<TechnicalDocument>();
        public DbSet<StateHistory> StateHistories => Set<StateHistory>();
        public DbSet<ReviewObservation> ReviewObservations => Set<ReviewObservation>();

        //public DbSet<AuditableEntity> AuditableEntities => Set<AuditableEntity>();

        public DbSet<Generator> Generators => Set<Generator>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Habilita la extensión para mapas/GPS en Postgres
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        }

    }
}
