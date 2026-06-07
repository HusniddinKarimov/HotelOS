using System.Collections.Concurrent;
using System.Data;
using HotelOS.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

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

    public async Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await work();
        await tx.CommitAsync(ct);
        return result;
    }
}
