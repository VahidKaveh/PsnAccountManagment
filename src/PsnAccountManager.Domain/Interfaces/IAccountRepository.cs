using PsnAccountManager.Domain.Entities;
using System.Linq.Expressions;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for Account entity operations
/// </summary>
public interface IAccountRepository : IGenericRepository<Account, int>
{
    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Fetches all active (not deleted, in stock) accounts and includes their related games
    /// </summary>
    Task<IEnumerable<Account>> GetActiveAccountsWithGamesAsync();

    /// <summary>
    /// Fetches a single account by its ID and includes its related games
    /// </summary>
    Task<Account?> GetAccountWithGamesAsync(int accountId);

    /// <summary>
    /// Finds an account by its unique external ID within a specific channel
    /// </summary>
    Task<Account?> GetByExternalIdAsync(int channelId, string externalId);

    /// <summary>
    /// Gets all accounts with complete details
    /// </summary>
    Task<IEnumerable<Account>> GetAllWithDetailsAsync();

    /// <summary>
    /// Gets a single account with all related details
    /// </summary>
    Task<Account?> GetAccountWithAllDetailsAsync(int accountId);


    // ==================== NEW METHODS ====================

    /// <summary>
    /// Finds an account by its exact title
    /// </summary>
    Task<Account?> GetByTitleAsync(string title);

    /// <summary>
    /// Searches accounts by title (partial match)
    /// </summary>
    Task<IEnumerable<Account>> SearchByTitleAsync(string searchTerm);

    /// <summary>
    /// Gets account by its source RawMessage ID
    /// </summary>
    Task<Account?> GetByRawMessageIdAsync(int rawMessageId);

    /// <summary>
    /// Gets recently processed accounts
    /// </summary>
    Task<IEnumerable<Account>> GetRecentlyProcessedAsync(int count = 50);

    /// <summary>
    /// Gets accounts where processing failed
    /// </summary>
    Task<IEnumerable<Account>> GetProcessingFailedAsync();

    /// <summary>
    /// Gets total account count
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// Gets active account count (not deleted, in stock)
    /// </summary>
    Task<int> GetActiveCountAsync();
}
