using PsnAccountManager.Domain.Entities;
using System.Threading.Tasks;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Defines the contract for data access operations related to the User entity.
/// It extends the generic repository for basic CRUD operations and adds user-specific methods.
/// </summary>
public interface IUserRepository : IGenericRepository<User, int>
{
    /// <summary>
    /// Finds a user by their unique Telegram ID.
    /// This is crucial for identifying users interacting via a bot.
    /// </summary>
    /// <param name="telegramId">The user's unique Telegram identifier.</param>
    /// <returns>The User entity if found; otherwise, null.</returns>
    Task<User?> GetByTelegramIdAsync(long telegramId);

    // In the future, you might add methods for pagination or searching:
    // Task<IEnumerable<User>> GetUsersPagedAsync(int pageNumber, int pageSize);
    // Task<IEnumerable<User>> SearchByUsernameAsync(string username);
}