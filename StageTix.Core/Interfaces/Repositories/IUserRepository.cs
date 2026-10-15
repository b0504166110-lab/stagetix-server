using StageTix.Core.Entities;

namespace StageTix.Core.Interfaces.Repositories;

public interface IUserRepository
{
    /// <summary>Read-only. Email is compared in lower case.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Read-only.</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    /// <summary>Marks the user for insertion. Saved by <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(User user);
}
