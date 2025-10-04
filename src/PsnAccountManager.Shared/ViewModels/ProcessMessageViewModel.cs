using System.ComponentModel.DataAnnotations;
using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Shared.ViewModels;

public class ProcessMessageViewModel
{
    [Required] public int RawMessageId { get; set; }

    [Required] [StringLength(500)] public string Title { get; set; } = string.Empty;

    [StringLength(5000)] public string? FullDescription { get; set; }
    public string? Region { get; set; }
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
  
    public string? SellerInfo { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? Capacity { get; set; }

    public List<string> GameTitles { get; set; }
}