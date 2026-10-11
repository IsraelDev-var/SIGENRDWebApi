using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using System.Reflection;
using System.Text.Json;

namespace SIGENRD.Infrastructure.Persistences.Contexts
{
    public class AppContextSIGENRD(DbContextOptions<AppContextSIGENRD> options, ICurrentUserService? currentUser = null) : DbContext(options)
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
        public DbSet<Generator> Generators => Set<Generator>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<NetMeteringSimulation> NetMeteringSimulations => Set<NetMeteringSimulation>();
        public DbSet<SimulationGenerator> SimulationGenerators => Set<SimulationGenerator>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Habilita la extensión para mapas/GPS en Postgres
            modelBuilder.HasPostgresExtension("postgis");

            // Búsqueda parcial por texto con índices trigram
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        // Solo se sobrescribe la versión asíncrona, que es la que usa UnitOfWork.
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var userId = currentUser?.UserId;
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                }
            }

            var pending = CaptureAudit();

            if (pending.Count == 0)
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            // El cambio y su auditoría se confirman juntos (RN-21): si falla el registro de auditoría, el cambio
            // se revierte. Si el llamador ya abrió una transacción, se usa la suya. El proveedor en memoria no
            // tiene transacciones.
            await using var transaction = Database.IsRelational() && Database.CurrentTransaction is null
                ? await Database.BeginTransactionAsync(cancellationToken)
                : null;

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            // Las claves de las entidades nuevas solo existen después del primer guardado.
            foreach (var item in pending)
            {
                var keyValues = item.Entry.Properties
                    .Where(p => p.Metadata.IsPrimaryKey())
                    .Select(p => p.CurrentValue);

                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = item.Action,
                    EntityName = item.EntityName,
                    EntityId = string.Join(",", keyValues),
                    OldValues = item.Old is null ? null : JsonSerializer.Serialize(item.Old),
                    NewValues = item.New is null ? null : JsonSerializer.Serialize(item.New),
                    CreatedAt = now
                });
            }

            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return result;
        }

        private sealed record AuditItem(EntityEntry Entry, string Action, string EntityName, Dictionary<string, object?>? Old, Dictionary<string, object?>? New);

        private List<AuditItem> CaptureAudit()
        {
            var items = new List<AuditItem>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // No se audita a sí misma, ni las notificaciones, ni la bandeja de correo (lleva códigos en el cuerpo).
                if (entry.Entity is AuditLog or Notification or OutboxEmail) continue;
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

                Dictionary<string, object?>? oldValues = null, newValues = null;

                foreach (var prop in entry.Properties)
                {
                    if (entry.State == EntityState.Added)
                    {
                        (newValues ??= new())[prop.Metadata.Name] = Plain(prop.CurrentValue);
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        (oldValues ??= new())[prop.Metadata.Name] = Plain(prop.OriginalValue);
                    }
                    else if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                    {
                        (oldValues ??= new())[prop.Metadata.Name] = Plain(prop.OriginalValue);
                        (newValues ??= new())[prop.Metadata.Name] = Plain(prop.CurrentValue);
                    }
                }

                // Modified sin cambios reales: no hay nada que auditar.
                if (entry.State == EntityState.Modified && newValues is null) continue;

                items.Add(new AuditItem(entry, entry.State.ToString(), entry.Metadata.ClrType.Name, oldValues, newValues));
            }

            return items;
        }

        // Las geometrías y los enums se guardan como texto para que el JSON sea legible.
        private static object? Plain(object? value) => value switch
        {
            Geometry g => g.AsText(),
            Enum e => e.ToString(),
            _ => value
        };
    }
}
