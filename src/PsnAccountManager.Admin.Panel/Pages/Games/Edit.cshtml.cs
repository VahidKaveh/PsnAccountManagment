using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PsnAccountManager.Admin.Panel.Pages.Games;

public class EditModel : PageModel
{
    private readonly IGameRepository _gameRepository;

    [BindProperty]
    public GameInputModel Input { get; set; }

    public class GameInputModel
    {
        // We need the ID to know which entity to update
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255)]
        public string Title { get; set; }

        [StringLength(100)]
        public string? SonyCode { get; set; }

        [StringLength(50)]
        public string? Region { get; set; }

        [StringLength(500)]
        [Url(ErrorMessage = "Please enter a valid URL for the poster.")]
        public string? PosterUrl { get; set; }
    }

    public EditModel(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var game = await _gameRepository.GetByIdAsync(id.Value);
        if (game == null)
        {
            return NotFound();
        }

        // Map the entity to our InputModel
        Input = new GameInputModel
        {
            Id = game.Id,
            Title = game.Title,
            SonyCode = game.SonyCode,
            Region = game.Region,
            PosterUrl = game.PosterUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var gameToUpdate = await _gameRepository.GetByIdAsync(Input.Id);
        if (gameToUpdate == null)
        {
            return NotFound();
        }

        // Map the updated values from the InputModel back to the entity
        gameToUpdate.Title = Input.Title;
        gameToUpdate.SonyCode = Input.SonyCode;
        gameToUpdate.Region = Input.Region;
        gameToUpdate.PosterUrl = Input.PosterUrl;

        _gameRepository.Update(gameToUpdate);
        await _gameRepository.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}