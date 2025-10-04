using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

public class AccountHistoryRepository : GenericRepository<AccountHistory, int>, IAccountHistoryRepository
{
    private readonly PsnAccountManagerDbContext _context;

    public AccountHistoryRepository(PsnAccountManagerDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<AccountHistory>> GetHistoryForAccountAsync(int accountId)
    {
        return await _context.AccountHistories
            .Where(h => h.AccountId == accountId)
            .OrderByDescending(h => h.ChangedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}