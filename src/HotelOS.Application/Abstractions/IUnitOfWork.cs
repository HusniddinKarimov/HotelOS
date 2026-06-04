namespace HotelOS.Application.Abstractions;

/// <summary>
/// Unit of Work: hands out repositories that share one DbContext/transaction and
/// commits them together with <see cref="SaveChangesAsync"/>.
/// </summary>
public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
