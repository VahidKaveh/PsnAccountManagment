using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Threading.Tasks;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Implements the IUserRepository for data access using Entity Framework Core.
/// </summary>
public class UserRepository : GenericRepository<User, int>, IUserRepository
{
    // The DbContext is inherited from the GenericRepository's constructor
    public UserRepository(PsnAccountManagerDbContext context) : base(context) { }

    /// <summary>
    /// Finds a user by their unique Telegram ID using an efficient database query.
    /// </summary>
    /// <param name="telegramId">The user's unique Telegram identifier.</param>
    /// <returns>The User entity if found; otherwise, null.</returns>
    public async Task<User?> GetByTelegramIdAsync(long telegramId)
    {
        // _dbSet is the DbSet<User> inherited from GenericRepository
        return await _dbSet
            .AsNoTracking() // Use AsNoTracking for read-only queries to improve performance
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }
}