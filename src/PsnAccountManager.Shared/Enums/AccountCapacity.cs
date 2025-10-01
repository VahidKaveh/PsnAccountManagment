using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;


/// <summary>
/// Represents the capacity/access level of a PlayStation account
/// </summary>
public enum AccountCapacity
{
    /// <summary>
    /// Unknown or not specified
    /// </summary>
    [Display(Name = "Unknown")]
    Unknown = 0,

    /// <summary>
    /// Primary account with full access (can be set as primary PS)
    /// </summary>
    [Display(Name = "Primary (Full Access)")]
    Primary = 1,

    /// <summary>
    /// Secondary account with limited access
    /// </summary>
    [Display(Name = "Secondary (Limited)")]
    Secondary = 2,

    /// <summary>
    /// Offline-only access (Z1 or similar)
    /// </summary>
    [Display(Name = "Offline Only (Z1)")]
    OfflineOnly = 3,

    /// <summary>
    /// Tertiary account with very limited access (optional)
    /// </summary>
    [Display(Name = "Tertiary")]
    Tertiary = 4
}
