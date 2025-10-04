using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

public class ManualCorrectionDto
{
    [Required] public int ChannelId { get; set; }

    public int? RawMessageId { get; set; }

    [Required] [StringLength(50)] public string EntityType { get; set; } = string.Empty;

    [Required] [StringLength(500)] public string EntityValue { get; set; } = string.Empty;

    public string? OriginalText { get; set; }
    public string? TextContext { get; set; }

    [Range(0, 100)] public int ConfidenceLevel { get; set; } = 100;

    public string? CorrectedBy { get; set; }
}