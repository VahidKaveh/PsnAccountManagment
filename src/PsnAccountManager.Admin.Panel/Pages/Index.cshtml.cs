using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces; // For IWorkerStateService
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.ViewModels;
using PsnAccountManager.Shared.Enums;
namespace PsnAccountManager.Admin.Panel.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IAccountRepository _accountRepo;
    private readonly IGameRepository _gameRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IWorkerStateService _workerState;

    // Properties for statistics
    public int TotalAccounts { get; private set; }
    public int ActiveAccounts { get; private set; }
    public int TotalGames { get; private set; }
    public int PendingPurchases { get; private set; }

    public WorkerStatusViewModel WorkerStatus { get; private set; }

    [TempData]
    public string StatusMessage { get; set; }

    public IndexModel(
        ILogger<IndexModel> logger,
        IAccountRepository accountRepo,
        IGameRepository gameRepo,
        IUserRepository userRepo,
        IPurchaseRepository purchaseRepo,
        IWorkerStateService workerState) // ✅ تزریق وابستگی جدید
    {
        _logger = logger;
        _accountRepo = accountRepo;
        _gameRepo = gameRepo;
        _userRepo = userRepo;
        _purchaseRepo = purchaseRepo;
        _workerState = workerState;
    }

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Dashboard page loading data.");

        // Fetching data sequentially
        var allAccounts = (await _accountRepo.GetAllAsync()).ToList();
        var allGames = await _gameRepo.GetAllAsync();
        var pendingPurchases = await _purchaseRepo.FindAsync(p => p.Status == PurchaseStatus.Pending);

        TotalAccounts = allAccounts.Count();
        ActiveAccounts = allAccounts.Count(a => !a.IsDeleted && a.StockStatus == StockStatus.InStock);
        TotalGames = allGames.Count();
        PendingPurchases = pendingPurchases.Count();

        // Fetch worker status
        WorkerStatus = _workerState.GetStatus();
    }

    // --- Page Handlers for Worker Control ---

    public IActionResult OnPostStopWorker()
    {
        _logger.LogWarning("Admin requested to STOP the Scraper Worker.");
        _workerState.Stop();
        StatusMessage = "Scraper Worker has been stopped.";
        return RedirectToPage();
    }

    public IActionResult OnPostStartWorker()
    {
        _logger.LogWarning("Admin requested to START the Scraper Worker.");
        _workerState.Start();
        StatusMessage = "Scraper Worker has been started.";
        return RedirectToPage();
    }
}