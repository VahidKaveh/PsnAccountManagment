using System.Linq.Expressions;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for Purchase entity operations
/// </summary>
public interface IPurchaseRepository : IGenericRepository<Purchase, int>
{
    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Gets the complete purchase history for a specific user
    /// </summary>
    Task<IEnumerable<Purchase>> GetPurchasesForUserAsync(int userId);

    /// <summary>
    /// Gets a single purchase with all related details
    /// </summary>
    Task<Purchase?> GetPurchaseWithDetailsAsync(int purchaseId);


    // ==================== NEW METHODS ====================

    /// <summary>
    /// Gets all purchases for a specific account
    /// </summary>
    Task<IEnumerable<Purchase>> GetPurchasesForAccountAsync(int accountId);

    /// <summary>
    /// Gets all purchases from a specific channel
    /// </summary>
    Task<IEnumerable<Purchase>> GetPurchasesForChannelAsync(int channelId);

    /// <summary>
    /// Gets purchases by status
    /// </summary>
    Task<IEnumerable<Purchase>> GetByStatusAsync(PurchaseStatus status);

    /// <summary>
    /// Gets total revenue from all completed purchases
    /// </summary>
    Task<decimal> GetTotalRevenueAsync();

    /// <summary>
    /// Gets total sales count
    /// </summary>
    Task<int> GetTotalSalesCountAsync();
}