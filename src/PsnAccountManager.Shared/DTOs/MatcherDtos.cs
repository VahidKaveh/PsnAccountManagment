namespace PsnAccountManager.Shared.DTOs;

/// <summary>
///     Input for the matching algorithm, containing the user's requested games.
/// </summary>
public class MatchRequestDto
{
    public List<int> RequestedGameIds { get; set; } = new();
}

/// <summary>
///     Represents a single account suggested by the matching algorithm.
/// </summary>
public class MatchedAccountDto
{
    public int AccountId { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public int MatchedGamesCount { get; set; }
    public List<GameDto> MatchedGames { get; set; } = new();
    public bool IsPrimarySuggestion { get; set; } // Flag to identify the best initial match
}

/// <summary>
///     The final result of the matching process, containing a ranked list of suggestions.
/// </summary>
public class MatchResultDto
{
    public List<MatchedAccountDto> Suggestions { get; set; } = new();
    public int RequestedGamesCount { get; set; }
    public int TotalGamesFound => Suggestions.Sum(s => s.MatchedGamesCount);
    public int TotalSuggestions => Suggestions.Count;
}

/// <summary>
///     Enum defining the sorting strategy for secondary suggestions, read from settings.
/// </summary>
public enum SuggestionSortOrder
{
    None,
    ByPrice, // Ascending
    ByMatchedGames // Descending
}