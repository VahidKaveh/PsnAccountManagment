using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;

/// <summary>
/// Represents the mode for fetching messages from Telegram
/// </summary>
public enum TelegramFetchMode
{
    /// <summary>
    /// Fetch messages since the last known message ID (default)
    /// </summary>
    [Display(Name = "Since Last Message (Default)")]
    SinceLastMessage = 0,

    /// <summary>
    /// Fetch the last X number of messages
    /// </summary>
    [Display(Name = "Last X Messages")] LastXMessages = 1,

    /// <summary>
    /// Fetch messages from the last X hours
    /// </summary>
    [Display(Name = "Since X Hours Ago")] SinceXHoursAgo = 2
}