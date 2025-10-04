using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// DTO for updating existing learning data (e.g., marking as used in training)
/// </summary>
public class UpdateLearningDataDto
{
    [Required(ErrorMessage = "Entity value is required.")]
    [StringLength(500)]
    [Display(Name = "Corrected Value")]
    public string EntityValue { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Text Context")]
    public string? TextContext { get; set; }

    [Range(0, 100)]
    [Display(Name = "Confidence Level")]
    public int ConfidenceLevel { get; set; }

    [Display(Name = "Mark as Used in Training")]
    public bool IsUsedInTraining { get; set; }
}