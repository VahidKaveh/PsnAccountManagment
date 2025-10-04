using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

public class PurchaseSuggestionRepository(PsnAccountManagerDbContext context)
    : GenericRepository<PurchaseSuggestion, int>(context), IPurchaseSuggestionRepository
{
    public async Task<IEnumerable<PurchaseSuggestion>> GetSuggestionsForUserAsync(int userId)
    {
        return await DbSet
            .Where(s => s.UserId == userId)
            .Include(s => s.Account) // Include the suggested account details
            .OrderByDescending(s => s.Rank) // Order by the best rank
            .ToListAsync();
    }
}