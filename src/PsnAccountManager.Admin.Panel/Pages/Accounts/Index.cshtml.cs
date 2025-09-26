using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Admin.Panel.Pages.Accounts;

public class IndexModel : PageModel
{
    private readonly IAccountRepository _accountRepository;

    public List<Account> Accounts { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public StockStatus? FilterStatus { get; set; }

    // Pagination properties
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 10;

    public IndexModel(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task OnGetAsync()
    {
        var allAccounts = (await _accountRepository.GetAllWithDetailsAsync()).AsQueryable(); // Assuming GetAllWithDetailsAsync in repo

        // Apply search filter
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            allAccounts = allAccounts.Where(a => a.Title.Contains(SearchTerm) || a.ExternalId.Contains(SearchTerm));
        }

        // Apply status filter
        if (FilterStatus.HasValue)
        {
            allAccounts = allAccounts.Where(a => a.StockStatus == FilterStatus.Value);
        }

        // Calculate total pages
        var totalRecords = allAccounts.Count();
        TotalPages = (int)System.Math.Ceiling(totalRecords / (double)PageSize);

        // Apply pagination
        Accounts = allAccounts
            .OrderByDescending(a => a.LastScrapedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
