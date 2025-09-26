using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.ViewModels;


namespace PsnAccountManager.Admin.Panel.Pages.Accounts;

public class EditModel : PageModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public AccountEditViewModel AccountVM { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public MultiSelectList AllGames { get; set; }

    public EditModel(IAccountRepository accountRepository, IGameRepository gameRepository, ILogger<EditModel> logger)
    {
        _accountRepository = accountRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var account = await _accountRepository.GetAccountWithGamesAsync(id);
        if (account == null) return NotFound();

        var selectedGameIds = account.AccountGames.Select(ag => ag.GameId).ToList();
        await LoadAllGames(selectedGameIds);

        AccountVM = new AccountEditViewModel
        {
            Id = account.Id,
            Title = account.Title,
            PricePs4 = account.PricePs4,
            PricePs5 = account.PricePs5,
            Region = account.Region,
            HasOriginalMail = account.HasOriginalMail,
            Capacity = account.Capacity,
            StockStatus = account.StockStatus,
            AdditionalInfo = account.AdditionalInfo,
            SelectedGameIds = selectedGameIds
        };
        // The AllGames property is now directly on the PageModel, not the VM
        // AccountVM.AllGames = this.AllGames; // This line was incorrect and is removed.

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAllGames(AccountVM.SelectedGameIds);
            return Page();
        }

        var accountToUpdate = await _accountRepository.GetAccountWithGamesAsync(AccountVM.Id);
        if (accountToUpdate == null) return NotFound();

        var changes = new List<string>();

        CheckAndRecordChange(changes, accountToUpdate, "Title", AccountVM.Title);
        CheckAndRecordChange(changes, accountToUpdate, "PricePs4", AccountVM.PricePs4);
        CheckAndRecordChange(changes, accountToUpdate, "PricePs5", AccountVM.PricePs5);
        CheckAndRecordChange(changes, accountToUpdate, "Region", AccountVM.Region);
        CheckAndRecordChange(changes, accountToUpdate, "HasOriginalMail", AccountVM.HasOriginalMail);
        CheckAndRecordChange(changes, accountToUpdate, "Capacity", AccountVM.Capacity);
        CheckAndRecordChange(changes, accountToUpdate, "StockStatus", AccountVM.StockStatus);
        CheckAndRecordChange(changes, accountToUpdate, "AdditionalInfo", AccountVM.AdditionalInfo);

        accountToUpdate.LastScrapedAt = DateTime.UtcNow;

        var existingGameIds = accountToUpdate.AccountGames.Select(ag => ag.GameId).ToHashSet();
        if (!existingGameIds.SetEquals(AccountVM.SelectedGameIds))
        {
            changes.Add("Games List");

            var gamesToRemove = accountToUpdate.AccountGames
                .Where(ag => !AccountVM.SelectedGameIds.Contains(ag.GameId)).ToList();
            foreach (var gameToRemove in gamesToRemove) accountToUpdate.AccountGames.Remove(gameToRemove);

            var newGameIds = AccountVM.SelectedGameIds.Where(id => !existingGameIds.Contains(id));
            foreach (var gameIdToAdd in newGameIds) accountToUpdate.AccountGames.Add(new AccountGame { GameId = gameIdToAdd });
        }

        if (changes.Any())
        {
            accountToUpdate.RecentChanges = string.Join(", ", changes);
        }

        _accountRepository.Update(accountToUpdate);
        await _accountRepository.SaveChangesAsync();

        StatusMessage = $"Account '{accountToUpdate.Title}' was updated successfully.";
        return RedirectToPage("./Details", new { id = accountToUpdate.Id });
    }

    private void CheckAndRecordChange<T>(List<string> changes, Account account, string fieldName, T newValue)
    {
        var prop = typeof(Account).GetProperty(fieldName);
        var oldValue = (T)prop.GetValue(account);

        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            changes.Add(fieldName);

            // Create a new history record
            account.History.Add(new AccountHistory
            {
                FieldName = fieldName,
                OldValue = oldValue?.ToString(),
                NewValue = newValue?.ToString(),
                ChangedBy = "Admin" // Assuming the logged-in user is an admin
            });

            prop.SetValue(account, newValue); // Update the entity's value
        }
    }

    private async Task LoadAllGames(List<int> selectedGameIds)
    {
        var allGames = await _gameRepository.GetAllAsync();
        AllGames = new MultiSelectList(allGames.OrderBy(g => g.Title), "Id", "Title", selectedGameIds);
    }

    private bool CheckForChange<T>(List<string> changes, string fieldName, T oldValue, T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            changes.Add(fieldName);
            return true; // Indicates that a change occurred
        }
        return false;
    }
}