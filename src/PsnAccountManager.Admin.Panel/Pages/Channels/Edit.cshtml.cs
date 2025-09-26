using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Channels;

public class EditModel : PageModel
{
    private readonly IChannelRepository _channelRepository;
    private readonly IParsingProfileRepository _profileRepository;

    [BindProperty]
    public InputModel Input { get; set; }

    public SelectList ParsingProfiles { get; set; }

    public class InputModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public ChannelStatus Status { get; set; }
        public int? ParsingProfileId { get; set; }

        [Required]
        [Display(Name = "Fetch Mode")]
        public FetchMode FetchMode { get; set; }

        [Display(Name = "Fetch Value (e.g., 100 messages or 24 hours)")]
        public int? FetchValue { get; set; }

        [Required]
        [Display(Name = "Delay After Scrape (milliseconds)")]
        [Range(0, 60000)]
        public int DelayAfterScrapeMs { get; set; }
    }

    public EditModel(IChannelRepository channelRepository, IParsingProfileRepository profileRepository)
    {
        _channelRepository = channelRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        var channel = await _channelRepository.GetByIdAsync(id.Value);
        if (channel == null) return NotFound();

        Input = new InputModel
        {
            Id = channel.Id,
            Name = channel.Name,
            Status = channel.Status,
            ParsingProfileId = channel.ParsingProfileId,
            FetchMode = channel.FetchMode,
            FetchValue = channel.FetchValue,
            DelayAfterScrapeMs = channel.DelayAfterScrapeMs
        };

        await LoadParsingProfiles();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParsingProfiles();
            return Page();
        }

        var channelToUpdate = await _channelRepository.GetByIdAsync(Input.Id);
        if (channelToUpdate == null) return NotFound();

        channelToUpdate.Name = Input.Name;
        channelToUpdate.Status = Input.Status;
        channelToUpdate.ParsingProfileId = Input.ParsingProfileId;
        channelToUpdate.FetchMode = Input.FetchMode;
        channelToUpdate.FetchValue = Input.FetchValue;
        channelToUpdate.DelayAfterScrapeMs = Input.DelayAfterScrapeMs;

        _channelRepository.Update(channelToUpdate);
        await _channelRepository.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadParsingProfiles()
    {
        var profiles = await _profileRepository.GetAllAsync();
        ParsingProfiles = new SelectList(profiles, nameof(ParsingProfile.Id), nameof(ParsingProfile.Name), Input?.ParsingProfileId);
    }
}