namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// DTO for displaying channels in admin panel
/// </summary>
public class ChannelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty; // Telegram username

    public string Status { get; set; } = string.Empty; // Active, Inactive, Paused
    public DateTime? LastScrapedAt { get; set; }
    public string? LastScrapedMessageId { get; set; }

    // Parsing Profile
    public int? ParsingProfileId { get; set; }
    public string? ParsingProfileName { get; set; }

    // Statistics
    public int TotalMessages { get; set; }
    public int PendingMessages { get; set; }
    public int ProcessedMessages { get; set; }
    public int TotalAccounts { get; set; }

    public DateTime CreatedAt { get; set; }
}