using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;

/// <summary>
/// Represents PlayStation platform types
/// </summary>
public enum Platform
{
    /// <summary>
    /// PlayStation 4 only
    /// </summary>
    [Display(Name = "PS4")] PS4 = 1,

    /// <summary>
    /// PlayStation 5 only
    /// </summary>
    [Display(Name = "PS5")] PS5 = 2,

    /// <summary>
    /// Works on both PS4 and PS5
    /// </summary>
    [Display(Name = "PS4 & PS5")] Both = 3
}