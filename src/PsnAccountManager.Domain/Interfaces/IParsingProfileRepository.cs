using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces
{
    public interface IParsingProfileRepository : IGenericRepository<ParsingProfile, int>
    {
        Task<ParsingProfile?> GetByIdWithRulesAsync(int id);
    }
}