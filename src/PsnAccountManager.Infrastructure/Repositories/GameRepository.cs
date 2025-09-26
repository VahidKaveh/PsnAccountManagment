using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsnAccountManager.Infrastructure.Repositories;

public class GameRepository(PsnAccountManagerDbContext context) : GenericRepository<Game, int>(context), IGameRepository
{
    public async Task<Game?> GetBySonyCodeAsync(string sonyCode)
    {
        return await _dbSet.FirstOrDefaultAsync(g => g.SonyCode == sonyCode);
    }

    public async Task<IEnumerable<Game>> SearchByTitleAsync(string titleQuery)
    {
        return await _dbSet
            .Where(g => g.Title.Contains(titleQuery))
            .ToListAsync();
    }

    public async Task<Game?> FindByTitleAsync(string title)
    {
        // Trim the input to remove leading/trailing whitespace
        var trimmedTitle = title.Trim();

        // Perform a case-insensitive search for an exact match.
        // EF Core can translate ToLower() to the appropriate SQL function (LOWER).
        return await _dbSet
            .FirstOrDefaultAsync(g => g.Title.ToLower() == trimmedTitle.ToLower());
    }
}