namespace PsnAccountManager.Shared.ViewModels;

public class ParsedGameViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? SonyCode { get; set; }
    public string? Region { get; set; }
    public bool ExistsInDb { get; set; }

    public static implicit operator string(ParsedGameViewModel game)
        => game.Title;

    public static implicit operator ParsedGameViewModel(string title)
        => new() { Title = title };
}