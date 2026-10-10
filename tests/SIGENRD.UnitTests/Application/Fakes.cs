using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.UnitTests.Application;

/// <summary>Repositorio genérico en memoria. Asigna ids al agregar.</summary>
public class FakeGenericRepository<T> : IGenericRepository<T> where T : class
{
    private int _nextId = 1;
    public List<T> Items { get; } = new();

    private static System.Reflection.PropertyInfo IdProperty => typeof(T).GetProperty("Id")!;

    public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(Items.ToList());

    public Task<T?> GetByIdAsync(int id) =>
        Task.FromResult(Items.FirstOrDefault(i => (int)IdProperty.GetValue(i)! == id));

    public Task AddAsync(T entity)
    {
        if ((int)IdProperty.GetValue(entity)! == 0) IdProperty.SetValue(entity, _nextId++);
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity) => Task.CompletedTask;
    public Task DeleteAsync(T entity) { Items.Remove(entity); return Task.CompletedTask; }
}

public class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();

    public int SaveCalls { get; private set; }

    public FakeGenericRepository<T> Fake<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repo))
            _repositories[typeof(T)] = repo = new FakeGenericRepository<T>();
        return (FakeGenericRepository<T>)repo;
    }

    public IGenericRepository<T> Repository<T>() where T : class => Fake<T>();

    public Task<int> SaveChangesAsync() { SaveCalls++; return Task.FromResult(1); }

    public void Dispose() { }
}

public class FakeTransformerRepository : ITransformerRepository
{
    public List<Transformer> Items { get; } = new();

    // Último filtro y límite recibidos, para comprobar lo que el handler le pide al repositorio
    public TransformerFilter? LastFilter { get; private set; }
    public int LastLimit { get; private set; }

    public Task<IReadOnlyList<Transformer>> GetInBoundsAsync(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude,
        TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        LastFilter = filter;
        LastLimit = limit;
        return Task.FromResult<IReadOnlyList<Transformer>>(Items.Take(limit).ToList());
    }

    public Task<IReadOnlyList<Transformer>> GetNearAsync(double latitude, double longitude, double radiusMeters,
        TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
    {
        LastFilter = filter;
        LastLimit = limit;
        return Task.FromResult<IReadOnlyList<Transformer>>(Items.Take(limit).ToList());
    }

    public Task<IReadOnlyList<Transformer>> SearchByCodeAsync(string term, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transformer>>(
            Items.Where(t => t.Code.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(limit).ToList());

    public Task<Transformer?> GetByIdAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlySet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var wanted = codes.ToHashSet();
        return Task.FromResult<IReadOnlySet<string>>(Items.Select(t => t.Code).Where(wanted.Contains).ToHashSet());
    }

    public Task AddAsync(Transformer transformer, CancellationToken cancellationToken = default)
    {
        if (transformer.Id == 0) transformer.Id = Items.Count + 1;
        Items.Add(transformer);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Transformer> transformers, CancellationToken cancellationToken = default)
    {
        foreach (var t in transformers) AddAsync(t, cancellationToken);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ZoneRiskSummary>> GetZoneRiskAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ZoneRiskSummary>>(Array.Empty<ZoneRiskSummary>());
}
