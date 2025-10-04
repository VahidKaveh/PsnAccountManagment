using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Admin.Panel.Pages.Users;

public class EditModel : PageModel
{
    private readonly IUserRepository _userRepository;

    [BindProperty] public UserInputModel Input { get; set; }

    public class UserInputModel
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; }

        [Required] public UserStatus Status { get; set; }
    }

    [TempData] public string StatusMessage { get; set; }

    public EditModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userRepository.GetByIdAsync(id.Value);
        if (user == null) return NotFound();

        Input = new UserInputModel
        {
            Id = user.Id,
            TelegramId = user.TelegramId,
            Username = user.Username,
            Status = user.Status
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userToUpdate = await _userRepository.GetByIdAsync(Input.Id);
        if (userToUpdate == null) return NotFound();

        userToUpdate.Status = Input.Status;
        // You can add logic to update other properties if needed

        _userRepository.Update(userToUpdate);
        await _userRepository.SaveChangesAsync();

        StatusMessage = $"User '{userToUpdate.Username}' has been updated successfully.";
        return RedirectToPage("./Index");
    }
}