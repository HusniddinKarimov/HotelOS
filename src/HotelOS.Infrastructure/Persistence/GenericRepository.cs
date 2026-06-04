using System.Linq.Expressions;
using HotelOS.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Infrastructure.Persistence;

/// <summary>EF Core implementation of the generic repository.</summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public IQueryable<T> Query(bool tracking = false) =>
        tracking ? _set.AsQueryable() : _set.AsNoTracking();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _set.FindAsync(new object?[] { id }, ct).AsTask();

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _set.FirstOrDefaultAsync(predicate, ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _set.AnyAsync(predicate, ct);

    public Task AddAsync(T entity, CancellationToken ct = default) =>
        _set.AddAsync(entity, ct).AsTask();

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);
}
