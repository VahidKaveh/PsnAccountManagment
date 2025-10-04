using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;

namespace PsnAccountManager.Admin.Panel.Pages.Games;

public class IndexModel : PageModel
{
    private readonly IGameRepository _gameRepository;

    public IList<Game> Games { get; set; }
    public int TotalGames { get; private set; }

    public IndexModel(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task OnGetAsync()
    {
        var allGames = (await _gameRepository.GetAllAsync()).ToList();

        Games = allGames;

        TotalGames = allGames.Count();
    }
}