using System.Threading.Tasks;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

public interface IRequestRepository : IGenericRepository<Request, int>
{
    // MatcherService needs to get a request with its associated games
    Task<Request?> GetRequestWithGamesAsync(int requestId);
}