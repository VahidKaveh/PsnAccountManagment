using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Represents a raw message received from a channel before processing
/// </summary>
public class RawMessage : BaseEntity<int>
{
    public int ChannelId { get; set; }
    public long ExternalMessageId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public RawMessageStatus Status { get; set; }

    // Processing Information
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingResult { get; set; }
    public int? AccountId { get; set; }

    // Navigation Properties
    public virtual Channel Channel { get; set; } = null!;
    public virtual Account? Account { get; set; }
    public virtual ICollection<LearningData> LearningData { get; set; } = new List<LearningData>();
}