using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces; // Assuming you create a specific repository
using PsnAccountManager.Infrastructure.Repositories;

namespace PsnAccountManager.Admin.Panel.Pages.ParsingProfiles;

public class IndexModel : PageModel
{
    private readonly IParsingProfileRepository _profileRepository;

    public IList<ParsingProfile> Profiles { get; set; }

    public IndexModel(IParsingProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task OnGetAsync()
    {
        Profiles = (await _profileRepository.GetAllAsync()).ToList();
    }
}