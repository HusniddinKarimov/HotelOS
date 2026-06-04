using System.Linq.Expressions;

namespace HotelOS.Application.Abstractions;

/// <summary>Repository pattern over an entity set, used together with <see cref="IUnitOfWork"/>.</summary>
public interface IGenericRepository<T> where T : class
{
    /// <summary>Composable query. Pass tracking=true when you intend to mutate results.</summary>
    IQueryable<T> Query(bool tracking = false);

    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
