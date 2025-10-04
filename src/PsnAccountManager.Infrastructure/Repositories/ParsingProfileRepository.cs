using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;

namespace PsnAccountManager.Infrastructure.Repositories;

public class ParsingProfileRepository : GenericRepository<ParsingProfile, int>, IParsingProfileRepository
{
    public ParsingProfileRepository(Data.PsnAccountManagerDbContext context) : base(context)
    {
    }

    public async Task<ParsingProfile?> GetByIdWithRulesAsync(int id)
    {
        return await DbSet.Include(p => p.Rules).FirstOrDefaultAsync(p => p.Id == id);
    }
}