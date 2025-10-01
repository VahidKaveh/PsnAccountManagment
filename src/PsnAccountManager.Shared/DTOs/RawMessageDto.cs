namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// DTO for displaying raw Telegram messages in admin panel
/// </summary>
public class RawMessageDto
{
    public int Id { get; set; }

    // Channel Info
    public int ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;

    // Message Data
    public string ExternalMessageId { get; set; } = string.Empty; // Telegram message ID
    public string MessageText { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }

    // Processing Status
    public string Status { get; set; } = string.Empty; // Pending, Processed, Failed, Ignored
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingResult { get; set; } // Success message or error details

    // Link to created account (if processed successfully)
    public int? AccountId { get; set; }
    public string? AccountTitle { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
}