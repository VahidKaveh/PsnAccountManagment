using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
public interface IRawMessageRepository : IGenericRepository<RawMessage, int>
{
    Task<IEnumerable<RawMessage>> GetPendingMessagesWithChannelAsync();
    Task<RawMessage?> GetByExternalIdAsync(int channelId, long externalMessageId);
}