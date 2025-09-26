using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

public class RawMessageRepository : GenericRepository<RawMessage, int>, IRawMessageRepository
{
    public RawMessageRepository(PsnAccountManagerDbContext context) : base(context) { }

    public async Task<IEnumerable<RawMessage>> GetPendingMessagesWithChannelAsync()
    {
        return await _dbSet
            .Where(m => m.Status == RawMessageStatus.Pending)
            .Include(m => m.Channel)
            .ToListAsync();
    }

    public async Task<RawMessage?> GetByExternalIdAsync(int channelId, long externalMessageId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.ExternalMessageId == externalMessageId);
    }
}