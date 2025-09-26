using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;

public enum FetchMode
{
    [Display(Name = "Since Last Message (Default)")]
    SinceLastMessage,

    [Display(Name = "Last X Messages")]
    LastXMessages,

    [Display(Name = "Since X Hours Ago")]
    SinceXHoursAgo
}