namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// DTO for displaying learning data in admin panel
/// Used for viewing ML training data and manual corrections
/// </summary>
public class LearningDataDto
{
    public int Id { get; set; }

    // Channel Info
    public int ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;

    // Source Message (optional)
    public int? RawMessageId { get; set; }

    // Extracted Data
    public string EntityType { get; set; } = string.Empty; // "Game", "PricePs4", "Region", etc.
    public string EntityValue { get; set; } = string.Empty; // The extracted value

    // Context
    public string? OriginalText { get; set; } // Full message text
    public string? TextContext { get; set; } // Surrounding text (50 chars on each side)

    // ML Metadata
    public int ConfidenceLevel { get; set; } // 0-100
    public bool IsManualCorrection { get; set; }
    public bool IsUsedInTraining { get; set; }

    // Audit
    public string? CreatedBy { get; set; } // "system" or admin username
    public DateTime CreatedAt { get; set; }
}