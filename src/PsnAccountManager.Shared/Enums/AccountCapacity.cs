using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;

/// <summary>
/// Represents the capacity/access level of a PlayStation account
/// </summary>
public enum AccountCapacity
{
    [Display(Name = "Unknown")]
    Unknown,

    [Display(Name = "Z1 (Offline Only)")]
    Z1,

    [Display(Name = "Z2 (Online & Offline)")]
    Z2,

    [Display(Name = "Z3 (Online Only)")]
    Z3
}