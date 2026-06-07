namespace HotelOS.Application.Abstractions;

/// <summary>
/// Unit of Work: hands out repositories that share one DbContext/transaction and
/// commits them together with <see cref="SaveChangesAsync"/>.
/// </summary>
public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="work"/> inside a Serializable database transaction.
    /// Serializable isolation lets the database guarantee that two people cannot
    /// successfully book the same room for overlapping dates at the same time.
    /// </summary>
    Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct = default);
}
