using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.ViewModels;

public class ProcessMessageViewModel
{
    [Required]
    public int RawMessageId { get; set; }

    [Required]
    public string Title { get; set; }

    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public string? Region { get; set; }
    public string? AdditionalInfo { get; set; }


    // We only need the titles of the games to find/create them in the database
    public List<string> GameTitles { get; set; } = new();

    // You can add other editable fields here if needed
    // public bool HasOriginalMail { get; set; }
    // public string CapacityInfo { get; set; }
}