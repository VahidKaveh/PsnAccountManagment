using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
namespace PsnAccountManager.Infrastructure.Repositories;

public class GuideRepository : GenericRepository<Guide, int>, IGuideRepository
{
    public GuideRepository(PsnAccountManagerDbContext context) : base(context) { }

    public async Task<IEnumerable<Guide>> GetActiveGuidesAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(g => g.IsActive)
            .ToListAsync();
    }
}