using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Account entity
/// Updated with processing status tracking and search capabilities
/// </summary>
public class AccountRepository : GenericRepository<Account, int>, IAccountRepository
{
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(
        PsnAccountManagerDbContext context,
        ILogger<AccountRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all active accounts for a specific channel that haven't been scraped recently (stale)
    /// </summary>
    public async Task<IEnumerable<Account>> GetStaleAccountsForChannelAsync(int channelId, DateTime cutoffDate)
    {
        try
        {
            return await DbSet
                .Where(a => a.ChannelId == channelId &&
                            !a.IsDeleted &&
                            a.StockStatus == StockStatus.InStock &&
                            a.LastScrapedAt < cutoffDate)
                .OrderBy(a => a.LastScrapedAt) // Order by oldest first
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stale accounts for channel {ChannelId} before {CutoffDate}",
                channelId, cutoffDate);
            return Enumerable.Empty<Account>();
        }
    }
    public async Task MarkAccountsAsRemovedAsync(int channelId, IEnumerable<string> existingExternalIds)
    {
        try
        {
            var existingIds = existingExternalIds.ToHashSet();

            // Find active accounts that are not in the current scrape results
            var accountsToRemove = await DbSet
                .Where(a => a.ChannelId == channelId &&
                           !a.IsDeleted &&
                           a.StockStatus == StockStatus.InStock &&
                           !existingIds.Contains(a.ExternalId))
                .ToListAsync();

            if (accountsToRemove.Any())
            {
                var removedCount = 0;
                foreach (var account in accountsToRemove)
                {
                    account.IsDeleted = true;
                    account.StockStatus = StockStatus.OutOfStock;
                    account.LastScrapedAt = DateTime.UtcNow;
                    account.Notes = $"Auto-removed: Not found in channel scrape at {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                    removedCount++;
                }

                await SaveChangesAsync();
                _logger.LogInformation("Marked {Count} accounts as removed for channel {ChannelId}",
                    removedCount, channelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking accounts as removed for channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsForChannelAsync(int channelId)
    {
        try
        {
            return await DbSet
                .AsNoTracking()
                .Where(a => a.ChannelId == channelId && !a.IsDeleted && a.StockStatus == StockStatus.InStock)
                .OrderByDescending(a => a.LastScrapedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active accounts for channel {ChannelId}", channelId);
            return Enumerable.Empty<Account>();
        }
    }

    // /// <summary>
    // ///  Gets a paginated list of accounts with filtering and sorting.
    // ///   </summary>
    public async Task<(List<Account> Accounts, int TotalCount)> GetPagedAccountsAsync(
        int pageNumber, int pageSize, string? searchTerm, StockStatus? status)
    {
        var query = Context.Accounts.Include(a => a.Channel).AsQueryable();

        // Apply filtering
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(lowerSearchTerm) ||
                                     a.ExternalId.ToLower().Contains(lowerSearchTerm));
        }

        if (status.HasValue) query = query.Where(a => a.StockStatus == status.Value);

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply sorting and pagination
        var accounts = await query
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (accounts, totalCount);
    }

    //   /// <summary>
    //   /// Gets statistics about all accounts.
    //   /// </summary>
    public async Task<AccountStatsViewModel> GetAccountStatsAsync()
    {
        var allAccounts = await Context.Accounts.ToListAsync();

        return new AccountStatsViewModel
        {
            TotalAccounts = allAccounts.Count,
            InStock = allAccounts.Count(a => a.StockStatus == StockStatus.InStock),
            Sold = allAccounts.Count(a => a.StockStatus == StockStatus.Sold),
            Reserved = allAccounts.Count(a => a.StockStatus == StockStatus.Reserved)
        };
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsWithGamesAsync()
    {
        try
        {
            return await DbSet
                .AsNoTracking()
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Where(a => !a.IsDeleted && a.StockStatus == StockStatus.InStock)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active accounts with games");
            throw;
        }
    }

    public async Task<Account?> GetAccountWithGamesAsync(int accountId)
    {
        try
        {
            return await DbSet
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account {AccountId} with games", accountId);
            throw;
        }
    }

    public async Task<Account?> GetByExternalIdAsync(int channelId, string externalId)
    {
        try
        {
            return await DbSet
                .FirstOrDefaultAsync(a => a.ChannelId == channelId && a.ExternalId == externalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by external ID {ExternalId} for channel {ChannelId}",
                externalId, channelId);
            throw;
        }
    }

    public async Task<IEnumerable<Account>> GetAllWithDetailsAsync()
    {
        try
        {
            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .OrderByDescending(a => a.LastScrapedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all accounts with details");
            throw;
        }
    }

    public async Task<Account?> GetAccountWithAllDetailsAsync(int accountId)
    {
        try
        {
            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Include(a => a.Purchases)
                .Include(a => a.History)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account {AccountId} with all details", accountId);
            throw;
        }
    }

    /// <summary>
    /// Finds an account by its exact title (case-insensitive)
    /// </summary>
    public async Task<Account?> GetByTitleAsync(string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Title is null or empty");
                return null;
            }

            var trimmedTitle = title.Trim();

            return await DbSet
                .Include(a => a.Channel)
                .FirstOrDefaultAsync(a => a.Title.ToLower() == trimmedTitle.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by title {Title}", title);
            throw;
        }
    }

    /// <summary>
    /// Searches accounts by title (partial match, case-insensitive)
    /// </summary>
    public async Task<IEnumerable<Account>> SearchByTitleAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("Search term is null or empty");
                return Enumerable.Empty<Account>();
            }

            var trimmedSearch = searchTerm.Trim().ToLower();

            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Where(a => a.Title.ToLower().Contains(trimmedSearch))
                .OrderByDescending(a => a.LastScrapedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching accounts by title {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Gets account by its source RawMessage ID
    /// Critical for tracking processing results
    /// </summary>
    public async Task<Account?> GetByRawMessageIdAsync(int rawMessageId)
    {
        try
        {
            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.RawMessage)
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .FirstOrDefaultAsync(a => a.RawMessageId == rawMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by raw message ID {RawMessageId}", rawMessageId);
            throw;
        }
    }

    /// <summary>
    /// Gets recently processed accounts
    /// Useful for admin dashboard and monitoring
    /// </summary>
    public async Task<IEnumerable<Account>> GetRecentlyProcessedAsync(int count = 50)
    {
        try
        {
            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Where(a => a.ProcessedAt != null)
                .OrderByDescending(a => a.ProcessedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently processed accounts");
            throw;
        }
    }

    /// <summary>
    /// Gets accounts where processing failed
    /// Critical for identifying and fixing processing issues
    /// </summary>
    public async Task<IEnumerable<Account>> GetProcessingFailedAsync()
    {
        try
        {
            return await DbSet
                .Include(a => a.Channel)
                .Include(a => a.RawMessage)
                .Where(a => a.ProcessingResult != null &&
                            a.ProcessingResult.Contains("failed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.ProcessedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts with processing failures");
            throw;
        }
    }

    /// <summary>
    /// Gets total count of all accounts
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await DbSet.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total account count");
            throw;
        }
    }

    /// <summary>
    /// Gets count of active accounts (not deleted, in stock)
    /// </summary>
    public async Task<int> GetActiveCountAsync()
    {
        try
        {
            return await DbSet
                .CountAsync(a => !a.IsDeleted && a.StockStatus == StockStatus.InStock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active account count");
            throw;
        }
    }

    // ============== NEW METHOD - پیاده‌سازی متد مفقود ==============
    
    /// <summary>
    /// Gets all accounts for a specific channel (for simple matching)
    /// </summary>
    public async Task<IEnumerable<Account>> GetByChannelIdAsync(int channelId)
    {
        try
        {
            return await DbSet
                .AsNoTracking()
                .Include(a => a.Channel)
                .Where(a => a.ChannelId == channelId)
                .OrderByDescending(a => a.LastScrapedAt ?? a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts for channel {ChannelId}", channelId);
            return Enumerable.Empty<Account>();
        }
    }
}