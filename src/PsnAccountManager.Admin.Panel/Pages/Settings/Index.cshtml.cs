using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Admin.Panel.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly ISettingRepository _settingRepository;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public List<Setting> Settings { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public IndexModel(ISettingRepository settingRepository, ILogger<IndexModel> logger)
        {
            _settingRepository = settingRepository;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var settings = await _settingRepository.GetAllAsync();
                Settings = settings.OrderBy(s => s.Key).ToList();

                _logger.LogInformation("Loaded {Count} application settings", Settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading application settings");
                Settings = new List<Setting>();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when updating settings");
                return Page();
            }

            try
            {
                _logger.LogInformation("Updating {Count} application settings", Settings.Count);

                var updatedCount = 0;
                foreach (var setting in Settings)
                {
                    if (string.IsNullOrWhiteSpace(setting.Key)) continue;

                    var existingSetting = await _settingRepository.GetByIdAsync(setting.Key);
                    if (existingSetting != null && existingSetting.Value != setting.Value)
                    {
                        var oldValue = existingSetting.Value;
                        existingSetting.Value = setting.Value?.Trim();
                        _settingRepository.Update(existingSetting);
                        updatedCount++;

                        _logger.LogInformation("Updated setting {Key}: {OldValue} → {NewValue}",
                            setting.Key, oldValue, existingSetting.Value);
                    }
                }

                if (updatedCount > 0)
                {
                    await _settingRepository.SaveChangesAsync();
                    StatusMessage = $"Successfully updated {updatedCount} setting(s).";
                    _logger.LogInformation("Successfully saved {Count} setting changes", updatedCount);
                }
                else
                {
                    StatusMessage = "No changes were made to the settings.";
                    _logger.LogInformation("No setting changes were detected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application settings");
                StatusMessage = "An error occurred while updating settings. Please try again.";
            }

            return RedirectToPage();
        }
    }
}
