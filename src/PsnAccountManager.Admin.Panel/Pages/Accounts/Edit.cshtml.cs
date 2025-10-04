using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Admin.Panel.Pages.Accounts;

public class EditModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IAccountService accountService, ILogger<EditModel> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [BindProperty] public AccountEditViewModel AccountVM { get; set; } = new();

    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null) return NotFound();

        AccountVM = new AccountEditViewModel
        {
            Id = account.Id,
            Title = account.Title,
            PricePs4 = account.PricePs4,
            PricePs5 = account.PricePs5,
            Region = account.Region,
            Capacity = account.Capacity,
            StockStatus = account.StockStatus,
            HasOriginalMail = account.HasOriginalMail,
            AdditionalInfo = account.AdditionalInfo,
            SellerInfo = account.SellerInfo,
            GameTitles = account.AccountGames.Select(ag => ag.Game.Title).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            var gameTitles = !string.IsNullOrEmpty(AccountVM.GameTitlesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(AccountVM.GameTitlesJson) ??
                  new List<string>()
                : new List<string>();

            var accountToUpdate = new Account
            {
                Id = AccountVM.Id,
                Title = AccountVM.Title,
                PricePs4 = AccountVM.PricePs4,
                PricePs5 = AccountVM.PricePs5,
                Region = AccountVM.Region,
                Capacity = AccountVM.Capacity,
                StockStatus = AccountVM.StockStatus,
                HasOriginalMail = AccountVM.HasOriginalMail,
                AdditionalInfo = AccountVM.AdditionalInfo,
                SellerInfo = AccountVM.SellerInfo
            };

            var updatedBy = User.Identity?.Name ?? "System";

            await _accountService.UpdateAccountAsync(accountToUpdate, gameTitles, updatedBy);

            StatusMessage = "Account updated successfully and changes have been logged.";
            return RedirectToPage("./Details", new { id = AccountVM.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account.");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the account.");
            return Page();
        }
    }
}