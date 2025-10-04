using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;

namespace PsnAccountManager.Admin.Panel.Pages.Users;

public class IndexModel : PageModel
{
    private readonly IUserRepository _userRepository;

    public IList<User> Users { get; set; }
    public int TotalUsers { get; set; }

    public IndexModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task OnGetAsync()
    {
        var allUsers = (await _userRepository.GetAllAsync()).ToList();
        Users = allUsers;
        TotalUsers = allUsers.Count;
    }
}