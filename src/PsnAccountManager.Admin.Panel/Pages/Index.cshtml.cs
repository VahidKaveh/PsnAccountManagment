using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAccountRepository _accountRepo;
        private readonly IGameRepository _gameRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly IWorkerStateService _workerState;

        // Properties for statistics
        public int TotalAccounts { get; private set; }
        public int ActiveAccounts { get; private set; }
        public int TotalGames { get; private set; }
        public int PendingPurchases { get; private set; }

        public WorkerStatusViewModel WorkerStatus { get; private set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            IAccountRepository accountRepo,
            IGameRepository gameRepo,
            IPurchaseRepository purchaseRepo,
            IWorkerStateService workerState)
        {
            _logger = logger;
            _accountRepo = accountRepo;
            _gameRepo = gameRepo;
            _purchaseRepo = purchaseRepo;
            _workerState = workerState;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Dashboard page loading initial data.");


            try
            {
                // Load statistics sequentially to avoid DbContext conflicts
                TotalAccounts = await _accountRepo.GetTotalCountAsync();
                _logger.LogDebug("Total accounts loaded: {TotalAccounts}", TotalAccounts);

                ActiveAccounts = await _accountRepo.GetActiveCountAsync();
                _logger.LogDebug("Active accounts loaded: {ActiveAccounts}", ActiveAccounts);

                TotalGames = await _gameRepo.GetTotalCountAsync();
                _logger.LogDebug("Total games loaded: {TotalGames}", TotalGames);

                var pendingPurchasesList = await _purchaseRepo.GetByStatusAsync(PurchaseStatus.Pending);
                PendingPurchases = pendingPurchasesList.Count();
                _logger.LogDebug("Pending purchases loaded: {PendingPurchases}", PendingPurchases);

                _logger.LogInformation("Dashboard statistics loaded successfully. Accounts: {TotalAccounts}, Active: {ActiveAccounts}, Games: {TotalGames}, Pending: {PendingPurchases}",
                    TotalAccounts, ActiveAccounts, TotalGames, PendingPurchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard statistics.");
                // Set default values on error
                TotalAccounts = 0;
                ActiveAccounts = 0;
                TotalGames = 0;
                PendingPurchases = 0;
            }

            // Load worker status separately to avoid affecting statistics
            try
            {
                WorkerStatus = _workerState?.GetStatus();
                if (WorkerStatus == null)
                {
                    _logger.LogWarning("WorkerStateService returned null status. Creating default status.");
                    WorkerStatus = CreateDefaultWorkerStatus();
                }
                else
                {
                    _logger.LogDebug("Worker status loaded: IsEnabled={IsEnabled}, Activity={Activity}",
                        WorkerStatus.IsEnabled, WorkerStatus.CurrentActivityMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading worker status.");
                WorkerStatus = CreateDefaultWorkerStatus();
            }
        }

        private WorkerStatusViewModel CreateDefaultWorkerStatus()
        {
            return new WorkerStatusViewModel
            {
                IsEnabled = false,
                CurrentActivityMessage = "Service unavailable",
                LastRunFinishedAt = null,
                LastRunDuration = null,
                MessagesFoundInLastRun = 0
            };
        }

        // --- Page Handlers for Worker Control (AJAX) ---

        public async Task<IActionResult> OnPostStopWorkerAsync()
        {
            try
            {
                _logger.LogWarning("Admin requested to STOP the Scraper Worker via AJAX.");

                if (_workerState == null)
                {
                    _logger.LogError("WorkerStateService is not available.");
                    return new BadRequestObjectResult(new { error = "Worker service not available." });
                }

                _workerState.Stop();
                _logger.LogInformation("Scraper Worker stop command completed successfully.");

                // Small delay to ensure status is updated
                await Task.Delay(500);

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping worker.");
                return new BadRequestObjectResult(new { error = "Failed to stop worker: " + ex.Message });
            }
        }

        public async Task<IActionResult> OnPostStartWorkerAsync()
        {
            try
            {
                _logger.LogWarning("Admin requested to START the Scraper Worker via AJAX.");

                if (_workerState == null)
                {
                    _logger.LogError("WorkerStateService is not available.");
                    return new BadRequestObjectResult(new { error = "Worker service not available." });
                }

                _workerState.Start();
                _logger.LogInformation("Scraper Worker start command completed successfully.");

                // Small delay to ensure status is updated
                await Task.Delay(500);

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting worker.");
                return new BadRequestObjectResult(new { error = "Failed to start worker: " + ex.Message });
            }
        }

        // --- API Endpoint for getting current worker status (for testing) ---
        public IActionResult OnGetWorkerStatus()
        {
            try
            {
                var status = _workerState?.GetStatus() ?? CreateDefaultWorkerStatus();
                return new JsonResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting worker status via API.");
                return new JsonResult(CreateDefaultWorkerStatus());
            }
        }
    }
}
