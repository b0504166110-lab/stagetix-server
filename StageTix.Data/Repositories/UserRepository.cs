using Microsoft.EntityFrameworkCore;
using StageTix.Core.Entities;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Data.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = Normalize(email);
        return db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = Normalize(email);
        return db.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
