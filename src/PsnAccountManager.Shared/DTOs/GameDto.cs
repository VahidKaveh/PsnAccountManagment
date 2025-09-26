namespace PsnAccountManager.Shared.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string SonyCode { get; set; }
    public string Title { get; set; }
    public string? Region { get; set; }
    public string? PosterUrl { get; set; }
}