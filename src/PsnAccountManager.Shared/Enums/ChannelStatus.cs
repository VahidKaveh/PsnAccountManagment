using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;


/// <summary>
/// Represents the operational status of a Telegram channel
/// </summary>
public enum ChannelStatus
{
    /// <summary>
    /// Channel is actively being scraped
    /// </summary>
    [Display(Name = "Active")]
    Active = 1,

    /// <summary>
    /// Channel is not being scraped
    /// </summary>
    [Display(Name = "Inactive")]
    Inactive = 2,

    /// <summary>
    /// Channel is temporarily paused by admin
    /// </summary>
    [Display(Name = "Paused")]
    Paused = 3,

    /// <summary>
    /// Channel has errors and needs attention
    /// </summary>
    [Display(Name = "Error")]
    Error = 4
}
