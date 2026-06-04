using System.Collections.Concurrent;
using HotelOS.Application.Abstractions;

namespace HotelOS.Infrastructure.Persistence;

/// <summary>Coordinates repositories over a single DbContext and commits them together.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext db) => _db = db;

    public IGenericRepository<T> Repository<T>() where T : class =>
        (IGenericRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_db));

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
