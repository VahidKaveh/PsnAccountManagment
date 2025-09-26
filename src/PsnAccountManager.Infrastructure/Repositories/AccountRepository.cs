using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

public class AccountRepository : GenericRepository<Account, int>, IAccountRepository
{
    public AccountRepository(PsnAccountManagerDbContext context) : base(context) { }

    public async Task<IEnumerable<Account>> GetActiveAccountsWithGamesAsync()
    {
        return await _dbSet
            .AsNoTracking() // Optimization: No need to track entities for a read-only list
            .Include(a => a.AccountGames)
            .ThenInclude(ag => ag.Game)
            .Where(a => !a.IsDeleted && a.StockStatus == StockStatus.InStock)
            .ToListAsync();
    }

    public async Task<Account?> GetAccountWithGamesAsync(int accountId)
    {
        return await _dbSet
            .Include(a => a.AccountGames)
            .ThenInclude(ag => ag.Game) // Include the Game details for each AccountGame
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<Account?> GetByExternalIdAsync(int channelId, string externalId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.ChannelId == channelId && a.ExternalId == externalId);
    }
    public async Task<IEnumerable<Account>> GetAllWithDetailsAsync()
    {
        return await _dbSet.Include(a => a.Channel).ToListAsync();
    }
    public async Task<Account?> GetAccountWithAllDetailsAsync(int accountId)
    {
        return await _dbSet
            .Include(a => a.Channel)
            .Include(a => a.AccountGames).ThenInclude(ag => ag.Game)
            .Include(a => a.History.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }
}