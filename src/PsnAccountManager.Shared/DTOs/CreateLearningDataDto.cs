using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

public class CreateLearningDataDto
{
    [Required(ErrorMessage = "Channel is required.")]
    public int ChannelId { get; set; }

    /// <summary>
    /// Optional: Link to source message
    /// </summary>
    public int? RawMessageId { get; set; }

    [Required(ErrorMessage = "Entity type is required.")]
    [StringLength(50, ErrorMessage = "Entity type cannot be longer than 50 characters.")]
    [Display(Name = "Entity Type")]
    public string EntityType { get; set; } = string.Empty; // "Game", "PricePs4", etc.

    [Required(ErrorMessage = "Entity value is required.")]
    [StringLength(500, ErrorMessage = "Entity value cannot be longer than 500 characters.")]
    [Display(Name = "Extracted Value")]
    public string EntityValue { get; set; } = string.Empty;

    [Display(Name = "Original Message Text")]
    public string? OriginalText { get; set; }

    [StringLength(500)]
    [Display(Name = "Text Context")]
    public string? TextContext { get; set; }

    /// <summary>
    /// Confidence level (0-100). Default 100 for manual corrections.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Confidence level must be between 0 and 100.")]
    [Display(Name = "Confidence Level")]
    public int ConfidenceLevel { get; set; } = 100;
}
