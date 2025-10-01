namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// DTO representing a single message fetched from Telegram
/// Used by TelegramClientWrapper to return message data
/// </summary>
public class TelegramMessageDto
{
    /// <summary>
    /// Telegram message ID
    /// </summary>
    public int ExternalMessageId { get; set; }

    /// <summary>
    /// Message text content
    /// </summary>
    public string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// When the message was posted on Telegram
    /// </summary>
    public DateTime ReceivedAt { get; set; }
}