using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Data Transfer Object for updating an existing account.
/// </summary>
public class UpdateAccountDto
{
    // The ID is typically passed via the URL route, not in the body,
    // but having it here can be useful for some patterns.

    [Required(ErrorMessage = "A title is required.")]
    [StringLength(500, ErrorMessage = "Title cannot be longer than 500 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "The full description from the post is required.")]
    public string Description { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price (PS4)")]
    public decimal? PricePs4 { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price (PS5)")]
    public decimal? PricePs5 { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [Display(Name = "Includes Original Mail")]
    public bool HasOriginalMail { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Guarantee must be a positive number.")]
    [Display(Name = "Guarantee (in minutes)")]
    public int? GuaranteeMinutes { get; set; }

    [StringLength(100)]
    [Display(Name = "Seller Info (e.g., Telegram @username)")]
    public string? SellerInfo { get; set; }

    [Required]
    public AccountCapacity Capacity { get; set; }

    [Required]
    [Display(Name = "Stock Status")]
    public StockStatus StockStatus { get; set; }

    [Display(Name = "Is Deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The full list of Game IDs that should be associated with this account after the update.
    /// The service layer will handle syncing this list.
    /// </summary>
    public List<int> GameIds { get; set; } = new();
}