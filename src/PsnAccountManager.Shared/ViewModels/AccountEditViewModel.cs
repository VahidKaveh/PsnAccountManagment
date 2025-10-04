using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Shared.ViewModels;

public class AccountEditViewModel
{
    public int Id { get; set; }

    [Required] public string Title { get; set; }

    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }

    public string? Region { get; set; }
    public bool HasOriginalMail { get; set; }
    public AccountCapacity Capacity { get; set; }
    public StockStatus StockStatus { get; set; }
    [Display(Name = "Seller Info")] public string SellerInfo { get; set; }
    [Display(Name = "Additional Info")] public string? AdditionalInfo { get; set; }

    [Display(Name = "Associated Games")] public List<int> SelectedGameIds { get; set; } = new();

    /// <summary>
    /// A comma-separated string of game titles for Bootstrap Tags Input.
    /// </summary>
    [Display(Name = "Associated Games")]
    public string? GamesAsTags { get; set; }

    public List<string> GameTitles { get; set; } = new();

    // This hidden field will carry the game titles as a JSON string
    public string GameTitlesJson { get; set; }
}