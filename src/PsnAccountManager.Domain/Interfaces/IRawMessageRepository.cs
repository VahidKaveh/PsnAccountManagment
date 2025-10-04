using System.Linq.Expressions;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for RawMessage entity operations
/// CRITICAL: This interface has been significantly improved
/// </summary>
public interface IRawMessageRepository : IGenericRepository<RawMessage, int>
{

    /// <summary>
    /// Gets all pending messages with their channel information
    /// </summary>
    Task<IEnumerable<RawMessage>> GetPendingMessagesWithChannelAsync();

    /// <summary>
    /// Finds a message by its external ID within a channel
    /// </summary>
    Task<RawMessage?> GetByExternalIdAsync(int channelId, string externalMessageId);

    Task<List<RawMessage>> GetInboxMessagesAsync(int skip , int take);

    /// <summary>
    /// Gets all pending messages that represent changes
    /// </summary>
    Task<IEnumerable<RawMessage>> GetPendingChangesAsync();

    /// <summary>
    /// Gets messages by status with pagination
    /// </summary>
    Task<IEnumerable<RawMessage>> GetByStatusAsync(RawMessageStatus status, int skip, int take);

    /// <summary>
    /// Counts messages by status
    /// </summary>
    Task<int> CountByStatusAsync(RawMessageStatus status);
    /// <summary>
    /// Gets messages by their status
    /// </summary>
    Task<IEnumerable<RawMessage>> GetByStatusAsync(RawMessageStatus status);

    /// <summary>
    /// Gets a message by ID with channel information
    /// </summary>
    Task<RawMessage?> GetByIdWithChannelAsync(int id);

    /// <summary>
    /// Gets recently processed messages
    /// </summary>
    Task<IEnumerable<RawMessage>> GetProcessedMessagesAsync(int count = 50);

    /// <summary>
    /// Gets messages where processing failed
    /// </summary>
    Task<IEnumerable<RawMessage>> GetFailedMessagesAsync();

    /// <summary>
    /// Gets ignored messages
    /// </summary>
    Task<IEnumerable<RawMessage>> GetIgnoredMessagesAsync();

    /// <summary>
    /// Gets count of pending messages
    /// </summary>
    Task<int> GetPendingCountAsync();

    /// <summary>
    /// Gets count of processed messages
    /// </summary>
    Task<int> GetProcessedCountAsync();

    /// <summary>
    /// Gets count of failed messages
    /// </summary>
    Task<int> GetFailedCountAsync();

    /// <summary>
    /// Marks a message as processed and links it to an account
    /// </summary>
    Task MarkAsProcessedAsync(int id, int? accountId = null);

    /// <summary>
    /// Marks a message as failed with error message
    /// </summary>
    Task MarkAsFailedAsync(int id, string error);

    /// <summary>
    /// Marks a message as ignored
    /// </summary>
    Task MarkAsIgnoredAsync(int id);
}