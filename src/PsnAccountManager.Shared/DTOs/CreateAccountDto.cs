using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Data Transfer Object for creating a new account manually via the admin panel.
/// </summary>
public class CreateAccountDto
{
    [Required] public int ChannelId { get; set; }

    [Required(ErrorMessage = "A title is required.")]
    [StringLength(500, ErrorMessage = "Title cannot be longer than 500 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "The full description from the post is required.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "The external ID (e.g., Telegram message ID) is required.")]
    [StringLength(100)]
    public string ExternalId { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price (PS4)")]
    public decimal? PricePs4 { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price (PS5)")]
    public decimal? PricePs5 { get; set; }

    [StringLength(100)] public string? Region { get; set; }

    [Display(Name = "Includes Original Mail")]
    public bool HasOriginalMail { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Guarantee must be a positive number.")]
    [Display(Name = "Guarantee (in minutes)")]
    public int? GuaranteeMinutes { get; set; }

    [StringLength(100)]
    [Display(Name = "Seller Info (e.g., Telegram @username)")]
    public string? SellerInfo { get; set; }

    [Required] public AccountCapacity Capacity { get; set; }

    [Required]
    [Display(Name = "Stock Status")]
    public StockStatus StockStatus { get; set; }

    /// <summary>
    /// A list of Game IDs to associate with this new account.
    /// </summary>
    public List<int> GameIds { get; set; } = new();

    public List<string> GameTitles { get; set; }

    [Display(Name = "Additional Info for this account")]
    public string AdditionalInfo { get; set; }
}