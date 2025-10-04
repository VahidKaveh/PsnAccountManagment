using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Admin.Panel.Pages.Accounts;

public class IndexModel : PageModel
{
    private readonly IAccountRepository _accountRepository;

    // Properties for the view
    public List<Account> Accounts { get; set; } = new();
    public AccountStatsViewModel AccountStats { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)] public StockStatus? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    [Range(1, int.MaxValue)]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 15;

    public IndexModel(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task OnGetAsync()
    {
        // Get statistics using the new repository method
        AccountStats = await _accountRepository.GetAccountStatsAsync();

        // Get paged accounts using the new repository method
        var (accounts, totalCount) = await _accountRepository.GetPagedAccountsAsync(
            CurrentPage,
            PageSize,
            SearchTerm,
            FilterStatus);

        Accounts = accounts;
        TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
    }
}