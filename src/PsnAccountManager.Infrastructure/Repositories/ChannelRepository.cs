using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Channel entity
/// Updated with comprehensive statistics and relationship loading
/// </summary>
public class ChannelRepository : GenericRepository<Channel, int>, IChannelRepository
{
    private readonly ILogger<ChannelRepository> _logger;

    public ChannelRepository(
        PsnAccountManagerDbContext context,
        ILogger<ChannelRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== EXISTING METHODS ====================

    public async Task<IEnumerable<Channel>> GetActiveChannelsAsync()
    {
        try
        {
            return await DbSet
                .Where(c => c.Status == ChannelStatus.Active)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels");
            throw;
        }
    }

    public async Task<Channel?> GetChannelWithProfileAsync(int channelId)
    {
        try
        {
            return await DbSet
                .Include(c => c.ParsingProfile)
                .ThenInclude(p => p.Rules)
                .FirstOrDefaultAsync(c => c.Id == channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel {ChannelId} with profile", channelId);
            throw;
        }
    }

    // ==================== NEW METHODS (5) ====================

    /// <summary>
    /// Gets channel with ALL related data including Profile, Accounts, Messages, and LearningData
    /// Useful for comprehensive channel analysis and admin dashboard
    /// </summary>
    public async Task<Channel?> GetChannelWithAllRelationsAsync(int channelId)
    {
        try
        {
            return await DbSet
                .Include(c => c.ParsingProfile)
                .ThenInclude(p => p.Rules)
                .Include(c => c.Accounts)
                .ThenInclude(a => a.AccountGames)
                .ThenInclude(ag => ag.Game)
                .Include(c => c.RawMessages)
                .AsSplitQuery() // Use split query for better performance with multiple collections
                .FirstOrDefaultAsync(c => c.Id == channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel {ChannelId} with all relations", channelId);
            throw;
        }
    }

    /// <summary>
    /// Gets total message count for a specific channel
    /// </summary>
    public async Task<int> GetMessageCountAsync(int channelId)
    {
        try
        {
            return await Context.Set<RawMessage>()
                .CountAsync(m => m.ChannelId == channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message count for channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Gets total account count for a specific channel
    /// </summary>
    public async Task<int> GetAccountCountAsync(int channelId)
    {
        try
        {
            return await Context.Set<Account>()
                .CountAsync(a => a.ChannelId == channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account count for channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Gets channels by their status
    /// Useful for filtering and management
    /// </summary>
    public async Task<IEnumerable<Channel>> GetByStatusAsync(ChannelStatus status)
    {
        try
        {
            return await DbSet
                .Include(c => c.ParsingProfile)
                .Where(c => c.Status == status)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channels by status {Status}", status);
            throw;
        }
    }
}