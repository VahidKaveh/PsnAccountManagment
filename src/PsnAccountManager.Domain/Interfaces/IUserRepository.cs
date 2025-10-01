using PsnAccountManager.Domain.Entities;
using System.Linq.Expressions;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository : IGenericRepository<User, int>
{
    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Finds a user by their unique Telegram ID
    /// </summary>
    Task<User?> GetByTelegramIdAsync(long telegramId);


    // ==================== NEW METHODS ====================

    /// <summary>
    /// Gets user with all purchases
    /// </summary>
    Task<User?> GetUserWithPurchasesAsync(int userId);

    /// <summary>
    /// Gets user with wishlist
    /// </summary>
    Task<User?> GetUserWithWishlistAsync(int userId);

    /// <summary>
    /// Gets count of active users
    /// </summary>
    Task<int> GetActiveUsersCountAsync();

    /// <summary>
    /// Gets recently active users
    /// </summary>
    Task<IEnumerable<User>> GetRecentlyActiveAsync(int count = 50);
}