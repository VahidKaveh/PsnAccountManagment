using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

public class RequestRepository(PsnAccountManagerDbContext context)
    : GenericRepository<Request, int>(context), IRequestRepository
{
    public async Task<Request?> GetRequestWithGamesAsync(int requestId)
    {
        return await DbSet
            .Include(r => r.RequestGames)
            .ThenInclude(rg => rg.Game) // Load the actual Game entity as well
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }
}