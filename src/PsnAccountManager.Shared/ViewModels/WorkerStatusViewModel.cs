using System;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Shared.ViewModels;

public class WorkerStatusViewModel
{
    public bool IsEnabled { get; set; }
    public WorkerActivity CurrentActivity { get; set; }
    public string CurrentActivityMessage { get; set; } = "Initializing...";
    public DateTime? LastRunFinishedAt { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public int? MessagesFoundInLastRun { get; set; }
}