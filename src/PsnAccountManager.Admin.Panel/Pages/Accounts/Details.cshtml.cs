using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Accounts
{
    public class DetailsModel : PageModel
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAccountHistoryRepository _historyRepository;

        public DetailsModel(
            IAccountRepository accountRepository,
            IAccountHistoryRepository historyRepository)
        {
            _accountRepository = accountRepository;
            _historyRepository = historyRepository;
        }

        public Account Account { get; set; }
        public List<AccountHistory> History { get; set; } = new List<AccountHistory>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Account = await _accountRepository.GetAccountWithAllDetailsAsync(id);

            if (Account == null)
            {
                return NotFound();
            }

            // Load the change history for this account separately
            History = await _historyRepository.GetHistoryForAccountAsync(id);

            return Page();
        }
    }
}