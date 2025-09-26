using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Application.Services;

/// <summary>
/// A Singleton service to control and report the state of background workers.
/// This implementation is thread-safe.
/// </summary>
public class WorkerStateService : IWorkerStateService
{
    private readonly object _lock = new object();
    private readonly WorkerStatusViewModel _status;

    public WorkerStateService()
    {
        // Initialize with default state
        _status = new WorkerStatusViewModel
        {
            IsEnabled = true, // Worker is enabled by default on startup
            CurrentActivity = WorkerActivity.Initializing,
            CurrentActivityMessage = "Worker is starting up..."
        };
    }

    public bool IsEnabled()
    {
        lock (_lock)
        {
            return _status.IsEnabled;
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            _status.IsEnabled = true;
            UpdateStatus(WorkerActivity.Idle, "Worker has been manually started.");
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _status.IsEnabled = false;
            UpdateStatus(WorkerActivity.Stopped, "Worker has been manually stopped by an admin.");
        }
    }

    public WorkerStatusViewModel GetStatus()
    {
        lock (_lock)
        {
            // Return a copy to prevent external modification
            return new WorkerStatusViewModel
            {
                IsEnabled = _status.IsEnabled,
                CurrentActivity = _status.CurrentActivity,
                CurrentActivityMessage = _status.CurrentActivityMessage,
                LastRunFinishedAt = _status.LastRunFinishedAt,
                LastRunDuration = _status.LastRunDuration,
                MessagesFoundInLastRun = _status.MessagesFoundInLastRun
            };
        }
    }

    public void UpdateStatus(WorkerActivity activity, string message)
    {
        lock (_lock)
        {
            _status.CurrentActivity = activity;
            _status.CurrentActivityMessage = message;
        }
    }

    public void ReportCycleCompletion(TimeSpan duration, int newMessagesCount)
    {
        lock (_lock)
        {
            _status.LastRunFinishedAt = DateTime.UtcNow;
            _status.LastRunDuration = duration;
            _status.MessagesFoundInLastRun = newMessagesCount;
        }
    }
}