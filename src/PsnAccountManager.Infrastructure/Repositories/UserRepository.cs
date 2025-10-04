using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity
/// Updated with purchase history, wishlist, and activity tracking
/// </summary>
public class UserRepository : GenericRepository<User, int>, IUserRepository
{
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        PsnAccountManagerDbContext context,
        ILogger<UserRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Finds a user by their unique Telegram ID
    /// </summary>
    public async Task<User?> GetByTelegramIdAsync(long telegramId)
    {
        try
        {
            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by Telegram ID {TelegramId}", telegramId);
            throw;
        }
    }

    // ==================== NEW METHODS (4) ====================

    /// <summary>
    /// Gets user with all purchase history
    /// Includes account details and games for each purchase
    /// </summary>
    public async Task<User?> GetUserWithPurchasesAsync(int userId)
    {
        try
        {
            return await DbSet
                .Include(u => u.Purchases)
                .ThenInclude(p => p.Account)
                .ThenInclude(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Include(u => u.Purchases)
                .ThenInclude(p => p.Payments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} with purchases", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets user with wishlist
    /// Useful for showing user's saved games
    /// </summary>
    public async Task<User?> GetUserWithWishlistAsync(int userId)
    {
        try
        {
            return await DbSet
                .Include(u => u.Wishlists)
                .ThenInclude(w => w.Game)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} with wishlist", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets count of active users
    /// Users with Active status
    /// </summary>
    public async Task<int> GetActiveUsersCountAsync()
    {
        try
        {
            return await DbSet
                .CountAsync(u => u.Status == UserStatus.Active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users count");
            throw;
        }
    }

    /// <summary>
    /// Gets recently active users
    /// Ordered by LastActiveAt descending
    /// Useful for user engagement analytics
    /// </summary>
    public async Task<IEnumerable<User>> GetRecentlyActiveAsync(int count = 50)
    {
        try
        {
            return await DbSet
                .AsNoTracking()
                .Where(u => u.Status == UserStatus.Active)
                .OrderByDescending(u => u.LastActiveAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently active users");
            throw;
        }
    }
}