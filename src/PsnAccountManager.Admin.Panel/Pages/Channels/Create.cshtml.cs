using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Channels;

public class CreateModel : PageModel
{
    private readonly IChannelRepository _channelRepository;
    private readonly IParsingProfileRepository _profileRepository;

    [BindProperty]
    public InputModel Input { get; set; }

    public SelectList ParsingProfiles { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Channel Name (e.g., 'psngames_sell')")]
        public string Name { get; set; }

        [Required]
        public ChannelStatus Status { get; set; }

        [Display(Name = "Parsing Profile")]
        public int? ParsingProfileId { get; set; }

        [Required]
        [Display(Name = "Fetch Mode")]
        public TelegramFetchMode TelegramFetchMode { get; set; }

        [Display(Name = "Fetch Value (e.g., 100 messages or 24 hours)")]
        public int? FetchValue { get; set; }

        [Required]
        [Display(Name = "Delay After Scrape (milliseconds)")]
        [Range(0, 60000)]
        public int DelayAfterScrapeMs { get; set; } = 1000;
    }

    public CreateModel(IChannelRepository channelRepository, IParsingProfileRepository profileRepository)
    {
        _channelRepository = channelRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IActionResult> OnGetAsync()
    {
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

        var newChannel = new Channel
        {
            Name = Input.Name,
            ExternalId = Input.Name,
            Status = Input.Status,
            ParsingProfileId = Input.ParsingProfileId,
            TelegramFetchMode = Input.TelegramFetchMode,
            FetchValue = Input.FetchValue,
            DelayAfterScrapeMs = Input.DelayAfterScrapeMs
        };

        await _channelRepository.AddAsync(newChannel);
        await _channelRepository.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadParsingProfiles()
    {
        var profiles = await _profileRepository.GetAllAsync();
        ParsingProfiles = new SelectList(profiles, nameof(ParsingProfile.Id), nameof(ParsingProfile.Name));
    }
}