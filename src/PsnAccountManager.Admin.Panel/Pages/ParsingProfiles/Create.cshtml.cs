using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;

namespace PsnAccountManager.Admin.Panel.Pages.ParsingProfiles;

public class CreateModel : PageModel
{
    private readonly IParsingProfileRepository _profileRepository;

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string ProfileName { get; set; }

    public CreateModel(IParsingProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var newProfile = new ParsingProfile { Name = ProfileName };
        await _profileRepository.AddAsync(newProfile);
        await _profileRepository.SaveChangesAsync();

        // Redirect to the Edit page to add rules immediately
        return RedirectToPage("./Edit", new { id = newProfile.Id });
    }
}