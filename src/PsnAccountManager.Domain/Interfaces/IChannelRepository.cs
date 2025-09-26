using PsnAccountManager.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsnAccountManager.Domain.Interfaces;

public interface IChannelRepository : IGenericRepository<Channel, int>
{
    // ScraperWorker needs to get all active channels to scrape
    Task<IEnumerable<Channel>> GetActiveChannelsAsync();
    Task<Channel?> GetChannelWithProfileAsync(int channelId);
}