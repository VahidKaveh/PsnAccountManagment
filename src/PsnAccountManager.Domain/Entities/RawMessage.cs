using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;

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


    /// <summary>
    /// SHA256 hash of normalized message content for change detection
    /// </summary>
    [MaxLength(100)]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Indicates if this message represents a change from a previous version
    /// </summary>
    public bool IsChange { get; set; } = false;

    /// <summary>
    /// ID of the previous RawMessage this change relates to
    /// </summary>
    public int? PreviousMessageId { get; set; }

    /// <summary>
    /// Navigation property to the previous version of this message
    /// </summary>
    public RawMessage? PreviousMessage { get; set; }

    /// <summary>
    /// JSON string containing details about what changed
    /// </summary>
    [MaxLength(2000)]
    public string? ChangeDetails { get; set; }

    public string ErrorMessage { get; set; }
}