using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Application.Interfaces;

public interface IWorkerStateService
{
    void Start();
    void Stop();
    bool IsEnabled();
    WorkerStatusViewModel GetStatus();
    void UpdateStatus(WorkerActivity activity, string message);
    void ReportCycleCompletion(TimeSpan duration, int newMessagesCount);
}