using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

public class ChannelRepository(PsnAccountManagerDbContext context) : GenericRepository<Channel, int>(context), IChannelRepository
{
    public async Task<IEnumerable<Channel>> GetActiveChannelsAsync()
    {
        return await _dbSet
            .Where(c => c.Status == ChannelStatus.Active)
            .ToListAsync();
    }
    public async Task<Channel?> GetChannelWithProfileAsync(int channelId)
    {
        return await _dbSet
            .Include(c => c.ParsingProfile)
            .ThenInclude(p => p.Rules)
            .FirstOrDefaultAsync(c => c.Id == channelId);
    }
}