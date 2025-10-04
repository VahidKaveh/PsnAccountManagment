using Microsoft.AspNetCore.SignalR;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Application.Hubs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System;

namespace PsnAccountManager.Application.Services
{
    public class WorkerStateService : IWorkerStateService
    {
        private readonly object _lock = new();
        private readonly WorkerStatusViewModel _status;
        private readonly IHubContext<DashboardHub> _hubContext;

        public WorkerStateService(IHubContext<DashboardHub> hubContext)
        {
            _hubContext = hubContext;
            _status = new WorkerStatusViewModel
            {
                // **FIX: Start with enabled=true since BackgroundService starts automatically**
                IsEnabled = true,  // تغییر از false به true
                CurrentActivity = WorkerActivity.Initializing,
                CurrentActivityMessage = "Worker is starting..."
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
                _status.CurrentActivity = WorkerActivity.Initializing;
                _status.CurrentActivityMessage = "Worker starting...";

                var status = GetStatus();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", status);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SignalR error: {ex.Message}");
                    }
                });
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _status.IsEnabled = false;
                _status.CurrentActivity = WorkerActivity.Stopped;
                _status.CurrentActivityMessage = "Worker stopped by admin";

                var status = GetStatus();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", status);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SignalR error: {ex.Message}");
                    }
                });
            }
        }

        public WorkerStatusViewModel GetStatus()
        {
            lock (_lock)
            {
                return new WorkerStatusViewModel
                {
                    IsEnabled = _status.IsEnabled,
                    CurrentActivity = _status.CurrentActivity,
                    CurrentActivityMessage = _status.CurrentActivityMessage ?? "Unknown",
                    LastRunFinishedAt = _status.LastRunFinishedAt,
                    LastRunDuration = _status.LastRunDuration,
                    MessagesFoundInLastRun = _status.MessagesFoundInLastRun ?? 0
                };
            }
        }

        public void UpdateStatus(WorkerActivity activity, string message)
        {
            lock (_lock)
            {
                _status.CurrentActivity = activity;
                _status.CurrentActivityMessage = message ?? "No message";

                var status = GetStatus();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", status);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SignalR error: {ex.Message}");
                    }
                });
            }
        }

        public void ReportCycleCompletion(TimeSpan duration, int newMessagesCount)
        {
            lock (_lock)
            {
                _status.LastRunFinishedAt = DateTime.UtcNow;
                _status.LastRunDuration = duration;
                _status.MessagesFoundInLastRun = newMessagesCount;

                var status = GetStatus();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", status);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SignalR error: {ex.Message}");
                    }
                });
            }
        }
    }
}
