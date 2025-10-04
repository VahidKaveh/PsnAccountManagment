using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Shared.Enums;
using System.Linq;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RawMessage entity
/// </summary>
public class RawMessageRepository : GenericRepository<RawMessage, int>, IRawMessageRepository
{
    private readonly ILogger<RawMessageRepository> _logger;

    public RawMessageRepository(
        PsnAccountManagerDbContext context,
        ILogger<RawMessageRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task<IEnumerable<RawMessage>> GetPendingMessagesWithChannelAsync()
    {
        try
        {
            return await DbSet
                .Where(m => m.Status == RawMessageStatus.Pending)
                .Include(m => m.Channel)
                .OrderBy(m => m.ReceivedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending messages with channel");
            throw;
        }
    }


    public async Task<IEnumerable<RawMessage>> GetByStatusAsync(RawMessageStatus status)
    {
        try
        {
            return await DbSet
                .Include(m => m.Channel)
                .Where(m => m.Status == status)
                .OrderByDescending(m => m.ReceivedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by status {Status}", status);
            throw;
        }
    }

    public async Task<RawMessage?> GetByIdWithChannelAsync(int id)
    {
        try
        {
            return await DbSet
                .Include(m => m.Channel)
                .ThenInclude(c => c.ParsingProfile)
                .ThenInclude(p => p.Rules)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message {MessageId} with channel", id);
            throw;
        }
    }

    public async Task<IEnumerable<RawMessage>> GetProcessedMessagesAsync(int count = 50)
    {
        try
        {
            return await DbSet
                .Include(m => m.Channel)
                .Include(m => m.Account)
                .Where(m => m.Status == RawMessageStatus.Processed)
                .OrderByDescending(m => m.ProcessedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processed messages");
            throw;
        }
    }

    public async Task<IEnumerable<RawMessage>> GetFailedMessagesAsync()
    {
        try
        {
            return await DbSet
                .Include(m => m.Channel)
                .Where(m => m.Status == RawMessageStatus.Failed)
                .OrderByDescending(m => m.ProcessedAt ?? m.ReceivedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed messages");
            throw;
        }
    }

    public async Task<IEnumerable<RawMessage>> GetIgnoredMessagesAsync()
    {
        try
        {
            return await DbSet
                .Include(m => m.Channel)
                .Where(m => m.Status == RawMessageStatus.Ignored)
                .OrderByDescending(m => m.ProcessedAt ?? m.ReceivedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ignored messages");
            throw;
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        try
        {
            return await DbSet
                .CountAsync(m => m.Status == RawMessageStatus.Pending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending message count");
            throw;
        }
    }

    public async Task<int> GetProcessedCountAsync()
    {
        try
        {
            return await DbSet
                .CountAsync(m => m.Status == RawMessageStatus.Processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processed message count");
            throw;
        }
    }

    public async Task<int> GetFailedCountAsync()
    {
        try
        {
            return await DbSet
                .CountAsync(m => m.Status == RawMessageStatus.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed message count");
            throw;
        }
    }

    public async Task MarkAsProcessedAsync(int id, int? accountId = null)
    {
        try
        {
            var message = await DbSet.FindAsync(id);
            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found for marking as processed", id);
                throw new InvalidOperationException($"Message with ID {id} not found.");
            }

            message.Status = RawMessageStatus.Processed;
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingResult = "Successfully processed";

            if (accountId.HasValue) message.AccountId = accountId.Value;

            DbSet.Update(message);
            await Context.SaveChangesAsync();

            _logger.LogInformation("Marked message {MessageId} as processed (Account: {AccountId})",
                id, accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as processed", id);
            throw;
        }
    }

    public async Task MarkAsFailedAsync(int id, string error)
    {
        try
        {
            var message = await DbSet.FindAsync(id);
            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found for marking as failed", id);
                throw new InvalidOperationException($"Message with ID {id} not found.");
            }

            message.Status = RawMessageStatus.Failed;
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingResult = string.IsNullOrWhiteSpace(error)
                ? "Processing failed"
                : error;

            DbSet.Update(message);
            await Context.SaveChangesAsync();

            _logger.LogWarning("Marked message {MessageId} as failed. Error: {Error}", id, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as failed", id);
            throw;
        }
    }

    public async Task MarkAsIgnoredAsync(int id)
    {
        try
        {
            var message = await DbSet.FindAsync(id);
            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found for marking as ignored", id);
                throw new InvalidOperationException($"Message with ID {id} not found.");
            }

            message.Status = RawMessageStatus.Ignored;
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingResult = "Manually ignored by admin";

            DbSet.Update(message);
            await Context.SaveChangesAsync();

            _logger.LogInformation("Marked message {MessageId} as ignored", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as ignored", id);
            throw;
        }
    }
    public async Task<IEnumerable<RawMessage>> GetPendingChangesAsync()
        {
            try
            {
                return await DbSet
                    .AsNoTracking()
                    .Include(rm => rm.Channel)
                    .Include(rm => rm.PreviousMessage)
                    .Where(rm => rm.Status == RawMessageStatus.PendingChange)
                    .OrderByDescending(rm => rm.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending changes");
                return Enumerable.Empty<RawMessage>();
            }
        }

        public async Task<IEnumerable<RawMessage>> GetByStatusAsync(RawMessageStatus status, int skip, int take)
        {
            try
            {
                return await DbSet
                    .AsNoTracking()
                    .Include(rm => rm.Channel)
                    .Include(rm => rm.PreviousMessage)
                    .Where(rm => rm.Status == status)
                    .OrderByDescending(rm => rm.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages by status {Status}", status);
                return Enumerable.Empty<RawMessage>();
            }
        }

        public async Task<int> CountByStatusAsync(RawMessageStatus status)
        {
            try
            {
                return await DbSet
                    .CountAsync(rm => rm.Status == status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting messages by status {Status}", status);
                return 0;
            }
        }

        public async Task<RawMessage?> GetByExternalIdAsync(int channelId, string externalId)
        {
            try
            {
                if (!long.TryParse(externalId, out var externalMessageId))
                {
                    _logger.LogWarning("Invalid external ID format: {ExternalId}", externalId);
                    return null;
                }

                return await DbSet
                    .AsNoTracking()
                    .Include(rm => rm.Channel)
                    .Where(rm => rm.ChannelId == channelId && rm.ExternalMessageId == externalMessageId)
                    .OrderByDescending(rm => rm.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message by external ID {ExternalId} in channel {ChannelId}",
                    externalId, channelId);
                return null;
            }
        }


}
