using PsnAccountManager.Shared.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.ViewModels;

public class ProcessMessageViewModel
{
    [Required]
    public int RawMessageId { get; set; }

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? FullDescription { get; set; }

    public bool IsSold { get; set; }

    // Extracted fields
    public List<string> ExtractedGames { get; set; } = new();
    public string? Region { get; set; }
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }
    public string? CapacityInfo { get; set; }
    public string? AdditionalInfo { get; set; }

    // ✅ NEW: این property را اضافه کن
    /// <summary>
    /// Manual corrections applied by admin
    /// </summary>
    public List<ManualCorrectionDto>? ManualCorrections { get; set; }
}