using System.Collections.Generic;
using System.Threading.Tasks;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

public interface IPurchaseSuggestionRepository : IGenericRepository<PurchaseSuggestion, int>
{
    // A potential useful method for fetching suggestions for a user
    Task<IEnumerable<PurchaseSuggestion>> GetSuggestionsForUserAsync(int userId);
}