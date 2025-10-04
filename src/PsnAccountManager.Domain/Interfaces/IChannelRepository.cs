using System.Linq.Expressions;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for Channel entity operations
/// </summary>
public interface IChannelRepository : IGenericRepository<Channel, int>
{
    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Gets all active channels for scraping
    /// </summary>
    Task<IEnumerable<Channel>> GetActiveChannelsAsync();

    /// <summary>
    /// Gets a channel with its parsing profile
    /// </summary>
    Task<Channel?> GetChannelWithProfileAsync(int channelId);

    /// <summary>
    /// Gets total message count for a channel
    /// </summary>
    Task<int> GetMessageCountAsync(int channelId);

    /// <summary>
    /// Gets total account count for a channel
    /// </summary>
    Task<int> GetAccountCountAsync(int channelId);

    /// <summary>
    /// Gets channels by status
    /// </summary>
    Task<IEnumerable<Channel>> GetByStatusAsync(ChannelStatus status);
}