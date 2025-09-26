using PsnAccountManager.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsnAccountManager.Domain.Interfaces;

public interface IPurchaseRepository : IGenericRepository<Purchase, int>
{
    /// <summary>
    /// Gets the complete purchase history for a specific user.
    /// </summary>
    Task<IEnumerable<Purchase>> GetPurchasesForUserAsync(int userId);

    /// <summary>
    /// Gets a single purchase with all related details (Buyer, Account, Payments).
    /// </summary>
    Task<Purchase?> GetPurchaseWithDetailsAsync(int purchaseId);
}