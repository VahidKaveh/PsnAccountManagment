using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;

namespace PsnAccountManager.Admin.Panel.Pages.Accounts;

public class DetailsModel : PageModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<DetailsModel> _logger;

    public Account Account { get; set; }

    public DetailsModel(IAccountRepository accountRepository, ILogger<DetailsModel> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        Account = await _accountRepository.GetAccountWithAllDetailsAsync(id.Value);
        if (Account == null) return NotFound();
        return Page();
    }
}