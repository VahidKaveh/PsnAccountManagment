using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Purchase entity
/// Updated with comprehensive purchase tracking and financial analytics
/// </summary>
public class PurchaseRepository : GenericRepository<Purchase, int>, IPurchaseRepository
{
    private readonly ILogger<PurchaseRepository> _logger;

    public PurchaseRepository(
        PsnAccountManagerDbContext context,
        ILogger<PurchaseRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== EXISTING METHODS ====================

    public async Task<IEnumerable<Purchase>> GetPurchasesForUserAsync(int userId)
    {
        try
        {
            return await DbSet
                .Where(p => p.BuyerUserId == userId)
                .Include(p => p.Account)
                .ThenInclude(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchases for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Purchase?> GetPurchaseWithDetailsAsync(int purchaseId)
    {
        try
        {
            return await DbSet
                .Include(p => p.Buyer)
                .Include(p => p.Account)
                .ThenInclude(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Include(p => p.Payments)
                .Include(p => p.SellerChannel)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == purchaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase {PurchaseId} with details", purchaseId);
            throw;
        }
    }

    // ==================== NEW METHODS (5) ====================

    /// <summary>
    /// Gets all purchases for a specific account
    /// Useful for tracking account purchase history
    /// </summary>
    public async Task<IEnumerable<Purchase>> GetPurchasesForAccountAsync(int accountId)
    {
        try
        {
            return await DbSet
                .Include(p => p.Buyer)
                .Include(p => p.Payments)
                .Where(p => p.AccountId == accountId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchases for account {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Gets all purchases from a specific channel
    /// Useful for channel revenue tracking and analytics
    /// </summary>
    public async Task<IEnumerable<Purchase>> GetPurchasesForChannelAsync(int channelId)
    {
        try
        {
            return await DbSet
                .Include(p => p.Buyer)
                .Include(p => p.Account)
                .ThenInclude(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Include(p => p.Payments)
                .Where(p => p.SellerChannelId == channelId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchases for channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Gets purchases by their status
    /// Useful for order management and filtering
    /// </summary>
    public async Task<IEnumerable<Purchase>> GetByStatusAsync(PurchaseStatus status)
    {
        try
        {
            return await DbSet
                .Include(p => p.Buyer)
                .Include(p => p.Account)
                .Include(p => p.SellerChannel)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchases by status {Status}", status);
            throw;
        }
    }

    /// <summary>
    /// Gets total revenue from all completed purchases
    /// Critical for financial reporting and analytics
    /// </summary>
    public async Task<decimal> GetTotalRevenueAsync()
    {
        try
        {
            var totalRevenue = await DbSet
                .Where(p => p.Status == PurchaseStatus.Completed)
                .SumAsync(p => p.TotalAmount);

            _logger.LogInformation("Total revenue calculated: {Revenue:C}", totalRevenue);

            return totalRevenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total revenue");
            throw;
        }
    }

    /// <summary>
    /// Gets total count of all sales
    /// Useful for business metrics and KPIs
    /// </summary>
    public async Task<int> GetTotalSalesCountAsync()
    {
        try
        {
            var count = await DbSet
                .CountAsync(p => p.Status == PurchaseStatus.Completed);

            _logger.LogInformation("Total sales count: {Count}", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total sales count");
            throw;
        }
    }
}