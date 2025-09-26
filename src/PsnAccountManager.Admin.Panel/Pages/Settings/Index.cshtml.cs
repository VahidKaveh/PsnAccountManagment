using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
namespace PsnAccountManager.Admin.Panel.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly ISettingRepository _settingRepository;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public List<Setting> Settings { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public IndexModel(ISettingRepository settingRepository, ILogger<IndexModel> logger)
    {
        _settingRepository = settingRepository;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        Settings = (await _settingRepository.GetAllAsync()).OrderBy(s => s.Key).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _logger.LogInformation("Updating application settings.");

        foreach (var setting in Settings)
        {
            var existingSetting = await _settingRepository.GetByIdAsync(setting.Key);
            if (existingSetting != null)
            {
                existingSetting.Value = setting.Value;
                _settingRepository.Update(existingSetting);
            }
        }

        await _settingRepository.SaveChangesAsync();
        _logger.LogInformation("Application settings updated successfully.");

        StatusMessage = "Settings have been updated successfully.";

        return RedirectToPage();
    }
}