namespace StageTix.Core.Interfaces.Repositories;

/// <summary>
/// Saves all pending changes of a request together, and runs work inside one database transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Saves all pending changes.</summary>
    /// <exception cref="Exceptions.ConcurrencyConflictException">
    /// A concurrency token did not match: someone else changed the same row after we read it.
    /// </exception>
    /// <exception cref="Exceptions.ConflictException">A unique constraint was violated.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="work"/> inside a database transaction: commits if it completes,
    /// rolls back if it throws. Used so the business check and the save happen together.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}
