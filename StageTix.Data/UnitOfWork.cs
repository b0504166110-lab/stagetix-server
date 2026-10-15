using Microsoft.EntityFrameworkCore;
using Npgsql;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Data;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // A concurrency token (ShowSeat.Version) did not match: another request saved
            // the same row after we read it. Translated to a Core exception so the Service
            // layer can handle it without depending on EF Core.
            throw new ConcurrencyConflictException(
                "The data was changed by another user at the same time. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("A record with the same unique value already exists.", ex);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        // Already inside a transaction (nested call): just join it.
        if (db.Database.CurrentTransaction is not null)
        {
            return await work(cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // If work throws, the transaction is disposed without a commit, which rolls it back.
        var result = await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
