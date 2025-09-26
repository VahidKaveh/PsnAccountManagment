namespace PsnAccountManager.Shared.Enums;

public enum WorkerActivity
{
    Initializing,
    Idle,
    Stopped,
    ConnectingToTelegram,
    Scraping,
    SavingData,
    CycleFinished,
    Error
}