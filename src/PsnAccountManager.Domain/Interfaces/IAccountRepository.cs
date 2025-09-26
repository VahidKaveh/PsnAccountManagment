using PsnAccountManager.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsnAccountManager.Domain.Interfaces;

public interface IAccountRepository : IGenericRepository<Account, int>
{
    /// <summary>
    /// Fetches all active (not deleted, in stock) accounts and includes their related games.
    /// Used by the MatcherService.
    /// </summary>
    Task<IEnumerable<Account>> GetActiveAccountsWithGamesAsync();

    /// <summary>
    /// Fetches a single account by its ID and includes its related games.
    /// Used by the AccountService to get detailed information.
    /// </summary>
    Task<Account?> GetAccountWithGamesAsync(int accountId); // ✅ متد مورد نیاز

    /// <summary>
    /// Finds an account by its unique external ID within a specific channel.
    /// Used by the ScraperService.
    /// </summary>
    Task<Account?> GetByExternalIdAsync(int channelId, string externalId);
    Task<IEnumerable<Account>> GetAllWithDetailsAsync();
    Task<Account?> GetAccountWithAllDetailsAsync(int accountId);
}