using System.Runtime.CompilerServices;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules.Simulation;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.Infrastructure.Persistences.Caching
{
    // Caché del mapa (RNF-01): el mapa es casi todo el tráfico y, en las pruebas de carga, la base de datos era el cuello
    // de botella. Todos los transformadores caben en memoria (10.000 según RNF-02 son unos pocos MB), así que los
    // encuadres y las búsquedas por cercanía se resuelven sobre una copia en memoria, sin consultar la base.

    public class MapCacheSettings
    {
        public const string SectionName = "MapCache";

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Vigencia de la copia. En esta instancia se descarta al instante cuando cambia un transformador; la vigencia
        /// acota cuánto tarda en verse el cambio en las demás instancias.
        /// </summary>
        public int TtlSeconds { get; set; } = 30;
    }

    /// <summary>Copia en memoria de todos los transformadores, ordenados por código. Solo lectura.</summary>
    public sealed class TransformerMapSnapshot(IReadOnlyList<Transformer> byCode, DateTimeOffset loadedAt)
    {
        public DateTimeOffset LoadedAt { get; } = loadedAt;
        public int Count => byCode.Count;

        /// <summary>Mismo resultado que la consulta de la base: dentro del rectángulo, filtrados, por código, hasta el límite.</summary>
        public IReadOnlyList<Transformer> InBounds(
            double minLat, double minLng, double maxLat, double maxLng, TransformerFilter filter, int limit)
        {
            var result = new List<Transformer>(Math.Min(limit, 256));
            foreach (var t in byCode)
            {
                var lat = t.Location.Y;
                var lng = t.Location.X;
                if (lat < minLat || lat > maxLat || lng < minLng || lng > maxLng || !Matches(t, filter)) continue;

                result.Add(t);
                if (result.Count == limit) break;
            }
            return result;
        }

        /// <summary>Dentro del radio, del más cercano al más lejano (distancia sobre la esfera, como Geo.DistanceMeters).</summary>
        public IReadOnlyList<Transformer> Near(double latitude, double longitude, double radiusMeters, TransformerFilter filter, int limit)
        {
            // Descarte rápido por un rectángulo que contiene al círculo; la distancia exacta solo para los que quedan.
            var dLat = radiusMeters / 111_000d;
            var dLng = radiusMeters / (111_000d * Math.Max(0.01, Math.Cos(latitude * Math.PI / 180)));

            return byCode
                .Where(t => Math.Abs(t.Location.Y - latitude) <= dLat && Math.Abs(t.Location.X - longitude) <= dLng)
                .Select(t => (Transformer: t, Distance: Geo.DistanceMeters(latitude, longitude, t.Location.Y, t.Location.X)))
                .Where(x => x.Distance <= radiusMeters && Matches(x.Transformer, filter))
                .OrderBy(x => x.Distance)
                .Take(limit)
                .Select(x => x.Transformer)
                .ToList();
        }

        /// <summary>Los mismos filtros que TransformerQueries.Filtered.</summary>
        private static bool Matches(Transformer t, TransformerFilter f) =>
            (f.DistributorId is null || t.DistributorId == f.DistributorId) &&
            (f.ServiceZoneId is null || t.ServiceZoneId == f.ServiceZoneId) &&
            (f.Status is null || t.Status == f.Status) &&
            (f.MinAvailabilityPct is null ||
             (t.TotalCapacityKva > 0 && (t.AvailableCapacityKva - t.ReservedCapacityKva) * 100m >= f.MinAvailabilityPct.Value * t.TotalCapacityKva));
    }

    /// <summary>
    /// Guarda la copia y la renueva al vencer o al invalidarse. Una sola carga a la vez: si llegan muchas peticiones
    /// con la copia vencida, solo una consulta la base y las demás esperan su resultado.
    /// </summary>
    public sealed class TransformerMapCache(IServiceScopeFactory scopes, MapCacheSettings settings, TimeProvider time)
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private TransformerMapSnapshot? _snapshot;
        private long _version;

        public async ValueTask<TransformerMapSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            var current = Volatile.Read(ref _snapshot);
            if (current is not null && !Expired(current)) return current;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                current = Volatile.Read(ref _snapshot);
                if (current is not null && !Expired(current)) return current;

                var version = Interlocked.Read(ref _version);
                var loaded = await LoadAsync(cancellationToken);

                // Si alguien la invalidó mientras se cargaba, lo leído puede ser anterior al cambio: se usa para esta
                // petición, pero no se guarda.
                if (version == Interlocked.Read(ref _version))
                    Volatile.Write(ref _snapshot, loaded);

                return loaded;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Cuántas veces se invalidó: sirve para diagnosticar y para las pruebas.</summary>
        public long Version => Interlocked.Read(ref _version);

        /// <summary>Descarta la copia: la próxima consulta la vuelve a cargar.</summary>
        public void Invalidate()
        {
            Interlocked.Increment(ref _version);
            Volatile.Write(ref _snapshot, null);
        }

        private bool Expired(TransformerMapSnapshot snapshot) =>
            time.GetUtcNow() - snapshot.LoadedAt >= TimeSpan.FromSeconds(settings.TtlSeconds);

        private async Task<TransformerMapSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppContextSIGENRD>();

            var all = await db.Transformers.AsNoTracking().ToListAsync(cancellationToken);
            all.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            return new TransformerMapSnapshot(all, time.GetUtcNow());
        }
    }

    /// <summary>
    /// Repositorio de transformadores con el mapa en caché: los encuadres y las búsquedas por cercanía salen de la copia
    /// en memoria; todo lo demás (detalle, búsqueda por código, escrituras) va a la base como siempre.
    /// Las entidades devueltas por esas dos consultas son compartidas: solo se leen.
    /// </summary>
    public class CachedTransformerRepository(TransformerRepository inner, TransformerMapCache cache, MapCacheSettings settings)
        : ITransformerRepository
    {
        public async Task<IReadOnlyList<Transformer>> GetInBoundsAsync(
            double minLatitude, double minLongitude, double maxLatitude, double maxLongitude,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
        {
            if (!settings.Enabled)
                return await inner.GetInBoundsAsync(minLatitude, minLongitude, maxLatitude, maxLongitude, filter, limit, cancellationToken);

            var snapshot = await cache.GetAsync(cancellationToken);
            return snapshot.InBounds(minLatitude, minLongitude, maxLatitude, maxLongitude, filter, limit);
        }

        public async Task<IReadOnlyList<Transformer>> GetNearAsync(
            double latitude, double longitude, double radiusMeters,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
        {
            if (!settings.Enabled)
                return await inner.GetNearAsync(latitude, longitude, radiusMeters, filter, limit, cancellationToken);

            var snapshot = await cache.GetAsync(cancellationToken);
            return snapshot.Near(latitude, longitude, radiusMeters, filter, limit);
        }

        public Task<IReadOnlyList<Transformer>> SearchByCodeAsync(string term, int limit, CancellationToken cancellationToken = default)
            => inner.SearchByCodeAsync(term, limit, cancellationToken);

        public Task<Transformer?> GetByIdAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default)
            => inner.GetByIdAsync(id, asTracking, cancellationToken);

        public Task<IReadOnlySet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
            => inner.GetExistingCodesAsync(codes, cancellationToken);

        public Task AddAsync(Transformer transformer, CancellationToken cancellationToken = default)
            => inner.AddAsync(transformer, cancellationToken);

        public Task AddRangeAsync(IEnumerable<Transformer> transformers, CancellationToken cancellationToken = default)
            => inner.AddRangeAsync(transformers, cancellationToken);

        public Task<IReadOnlyList<ZoneRiskSummary>> GetZoneRiskAsync(CancellationToken cancellationToken = default)
            => inner.GetZoneRiskAsync(cancellationToken);
    }

    /// <summary>
    /// Invalida la caché del mapa cuando se guarda un cambio en algún transformador (alta, capacidad, mantenimiento,
    /// importación, reservas del flujo de solicitudes). Si el guardado va dentro de una transacción, espera al commit:
    /// invalidar antes dejaría que otra petición recargara los datos viejos.
    /// </summary>
    public sealed class TransformerChangeInterceptor(TransformerMapCache cache) : SaveChangesInterceptor, IDbTransactionInterceptor
    {
        private static readonly ConditionalWeakTable<DbContext, object> Pending = new();
        private static readonly object Marker = new();

        private static void Track(DbContext? context)
        {
            if (context is null) return;
            if (context.ChangeTracker.Entries<Transformer>().Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                Pending.AddOrUpdate(context, Marker);
        }

        private void AfterSave(DbContext? context)
        {
            if (context is null || !Pending.TryGetValue(context, out _)) return;

            // Dentro de una transacción se espera a que se confirme.
            if (context.Database.CurrentTransaction is not null) return;

            Pending.Remove(context);
            cache.Invalidate();
        }

        private void Committed(DbContext? context)
        {
            if (context is not null && Pending.Remove(context))
                cache.Invalidate();
        }

        private static void Discard(DbContext? context)
        {
            if (context is not null) Pending.Remove(context);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Track(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Track(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            AfterSave(eventData.Context);
            return result;
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            AfterSave(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            if (eventData.Context?.Database.CurrentTransaction is null) Discard(eventData.Context);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.Database.CurrentTransaction is null) Discard(eventData.Context);
            return Task.CompletedTask;
        }

        public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) => Committed(eventData.Context);

        public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Committed(eventData.Context);
            return Task.CompletedTask;
        }

        public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) => Discard(eventData.Context);

        public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Discard(eventData.Context);
            return Task.CompletedTask;
        }
    }
}
