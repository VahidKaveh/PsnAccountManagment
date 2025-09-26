using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Games;

public class CreateModel : PageModel
{
    private readonly IGameRepository _gameRepository;

    [BindProperty]
    public GameInputModel Input { get; set; }

    public class GameInputModel
    {
        [Required]
        public string Title { get; set; }
        public string? SonyCode { get; set; }
        public string? Region { get; set; }
        public string? PosterUrl { get; set; }
    }

    public CreateModel(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var newGame = new Game
        {
            Title = Input.Title,
            SonyCode = Input.SonyCode,
            Region = Input.Region,
            PosterUrl = Input.PosterUrl
        };

        await _gameRepository.AddAsync(newGame);
        await _gameRepository.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}