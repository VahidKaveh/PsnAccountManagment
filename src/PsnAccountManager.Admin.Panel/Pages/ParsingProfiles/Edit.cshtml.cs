using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.ViewModels; 

namespace PsnAccountManager.Admin.Panel.Pages.ParsingProfiles;

public class EditModel : PageModel
{
    private readonly IParsingProfileRepository _profileRepository;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public ParsingProfileEditViewModel ProfileVM { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public EditModel(IParsingProfileRepository profileRepository, ILogger<EditModel> logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var profileEntity = await _profileRepository.GetByIdWithRulesAsync(id);
        if (profileEntity == null) return NotFound();

        // --- Map Entity to ViewModel ---
        ProfileVM = new ParsingProfileEditViewModel
        {
            Id = profileEntity.Id,
            Name = profileEntity.Name,
            Rules = profileEntity.Rules.Select(r => new ParsingProfileRuleViewModel
            {
                Id = r.Id,
                ParsingProfileId = r.ParsingProfileId,
                FieldType = r.FieldType,
                RegexPattern = r.RegexPattern
            }).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Log errors if necessary
            _logger.LogError("OnPostAsync failed due to invalid ModelState.");
            return Page();
        }

        var profileToUpdate = await _profileRepository.GetByIdWithRulesAsync(ProfileVM.Id);
        if (profileToUpdate == null) return NotFound();

        // --- Map ViewModel back to Entity ---
        profileToUpdate.Name = ProfileVM.Name;

        // Sync rules
        // 1. Remove deleted rules
        var submittedRuleIds = ProfileVM.Rules.Select(r => r.Id).ToList();
        var rulesToDelete = profileToUpdate.Rules.Where(r => !submittedRuleIds.Contains(r.Id)).ToList();
        foreach (var rule in rulesToDelete)
        {
            profileToUpdate.Rules.Remove(rule);
        }

        // 2. Update existing and add new rules
        foreach (var ruleVM in ProfileVM.Rules)
        {
            if (ruleVM.Id != 0) // Existing rule
            {
                var existingRule = profileToUpdate.Rules.First(r => r.Id == ruleVM.Id);
                existingRule.RegexPattern = ruleVM.RegexPattern;
            }
            else // New rule
            {
                profileToUpdate.Rules.Add(new Domain.Entities.ParsingProfileRule
                {
                    ParsingProfileId = profileToUpdate.Id,
                    FieldType = ruleVM.FieldType,
                    RegexPattern = ruleVM.RegexPattern
                });
            }
        }

        await _profileRepository.SaveChangesAsync();

        StatusMessage = $"Profile '{profileToUpdate.Name}' has been updated successfully.";
        return RedirectToPage(new { id = profileToUpdate.Id });
    }
}