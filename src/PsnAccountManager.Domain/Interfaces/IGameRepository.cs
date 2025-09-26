using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

public interface IGameRepository : IGenericRepository<Game, int>
{
    /// <summary>
    /// Finds a game by its unique identifier from Sony.
    /// </summary>
    Task<Game?> GetBySonyCodeAsync(string sonyCode);

    /// <summary>
    /// Searches for games based on their title.
    /// </summary>
    Task<IEnumerable<Game>> SearchByTitleAsync(string titleQuery);

    /// <summary>
    /// Finds a single game by its exact title (case-insensitive).
    /// Used by the ProcessingService to check for existing games before creating a new one.
    /// </summary>
    /// <param name="title">The exact title of the game to find.</param>
    /// <returns>The Game entity if found; otherwise, null.</returns>
    Task<Game?> FindByTitleAsync(string title);
}