namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Stores learning data for machine learning training
/// Used to improve automatic parsing accuracy over time
/// </summary>
public class LearningData : BaseEntity<int>
{
    public int ChannelId { get; set; }
    public int RawMessageId { get; set; }

    // Entity Information
    public string EntityType { get; set; } = string.Empty; // "game", "price", "region", "title", etc.
    public string EntityValue { get; set; } = string.Empty;

    // Context Information
    public string? OriginalText { get; set; }
    public string? TextContext { get; set; } // Text surrounding the entity

    // Learning Metadata
    public int ConfidenceLevel { get; set; } // 1-5 (5 is highest)
    public bool IsManualCorrection { get; set; }
    public bool IsUsedInTraining { get; set; }

    // Audit
    public string? CreatedBy { get; set; } ="Admin";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Channel? Channel { get; set; } = null!;
    public virtual RawMessage? RawMessage { get; set; }
}