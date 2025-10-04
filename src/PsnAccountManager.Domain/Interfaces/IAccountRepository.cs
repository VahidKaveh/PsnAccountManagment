using System.Linq.Expressions;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for Account entity operations
/// </summary>
public interface IAccountRepository : IGenericRepository<Account, int>
{

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


    /// <summary>
    /// Gets a paginated list of accounts with filtering and sorting options.
    /// </summary>
    Task<(List<Account> Accounts, int TotalCount)> GetPagedAccountsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        StockStatus? status);

    /// <summary>
    /// Gets statistics about all accounts in the database.
    /// </summary>
    Task<AccountStatsViewModel> GetAccountStatsAsync();


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

    /// <summary>
    /// Gets all active accounts for a specific channel that haven't been scraped recently
    /// </summary>
    Task<IEnumerable<Account>> GetStaleAccountsForChannelAsync(int channelId, DateTime cutoffDate);

    /// <summary>
    /// Marks accounts as deleted/out of stock if they're not found in current scrape
    /// </summary>
    Task MarkAccountsAsRemovedAsync(int channelId, IEnumerable<string> existingExternalIds);

    /// <summary>
    /// Gets all active accounts for a specific channel
    /// </summary>
    Task<IEnumerable<Account>> GetActiveAccountsForChannelAsync(int channelId);

    /// <summary>
    /// Gets all accounts for a specific channel (for simple matching)
    /// </summary>
    Task<IEnumerable<Account>> GetByChannelIdAsync(int channelId);

}