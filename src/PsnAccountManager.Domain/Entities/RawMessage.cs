using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

public class RawMessage : BaseEntity<int>
{
    public int ChannelId { get; set; }
    public long ExternalMessageId { get; set; } // The message_id from Telegram
    public string MessageText { get; set; }
    public DateTime ReceivedAt { get; set; }
    public RawMessageStatus Status { get; set; }

    public virtual Channel Channel { get; set; }
}