using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.Enums;


/// <summary>
/// Represents the current activity state of the ScraperWorker
/// </summary>
public enum WorkerActivity
{
    /// <summary>
    /// Worker is initializing
    /// </summary>
    [Display(Name = "Initializing")]
    Initializing = 0,

    /// <summary>
    /// Worker is idle, waiting for next cycle
    /// </summary>
    [Display(Name = "Idle")]
    Idle = 1,

    /// <summary>
    /// Worker has been stopped
    /// </summary>
    [Display(Name = "Stopped")]
    Stopped = 2,

    /// <summary>
    /// Worker is authenticating with Telegram
    /// </summary>
    [Display(Name = "Authenticating")]
    Authenticating = 3,

    /// <summary>
    /// Worker is actively scraping channels
    /// </summary>
    [Display(Name = "Scraping")]
    Scraping = 4,

    /// <summary>
    /// Worker is saving scraped data to database
    /// </summary>
    [Display(Name = "Saving Data")]
    SavingData = 5,

    /// <summary>
    /// Worker has finished a scraping cycle
    /// </summary>
    [Display(Name = "Cycle Finished")]
    CycleFinished = 6,

    /// <summary>
    /// Worker encountered an error
    /// </summary>
    [Display(Name = "Error")]
    Error = 7
}