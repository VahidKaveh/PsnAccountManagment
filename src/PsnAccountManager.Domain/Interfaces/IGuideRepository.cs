using PsnAccountManager.Domain.Entities;
namespace PsnAccountManager.Domain.Interfaces;

public interface IGuideRepository : IGenericRepository<Guide, int>
{
    /// <summary>
    /// Fetches all guides that are currently marked as active.
    /// </summary>
    Task<IEnumerable<Guide>> GetActiveGuidesAsync();
}