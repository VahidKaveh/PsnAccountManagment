using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsnAccountManager.Infrastructure.Repositories;

public class PurchaseRepository(PsnAccountManagerDbContext context) : GenericRepository<Purchase, int>(context), IPurchaseRepository
{
    public async Task<IEnumerable<Purchase>> GetPurchasesForUserAsync(int userId)
    {
        return await _dbSet
            .Where(p => p.BuyerUserId == userId)
            .Include(p => p.Account) // Include account details for the history
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Purchase?> GetPurchaseWithDetailsAsync(int purchaseId)
    {
        return await _dbSet
            .Include(p => p.Buyer)
            .Include(p => p.Account)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);
    }
}