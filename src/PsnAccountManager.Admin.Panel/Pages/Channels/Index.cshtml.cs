using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Channels;

public class IndexModel : PageModel
{
    private readonly IChannelRepository _channelRepository;

    public IList<Channel> Channels { get; set; }

    public IndexModel(IChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public async Task OnGetAsync()
    {
        Channels = (await _channelRepository.GetAllAsync()).ToList();
    }
}