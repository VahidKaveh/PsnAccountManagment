using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

public class CreateGameDto
{
    [Required(ErrorMessage = "Sony Code is required.")]
    [StringLength(100, ErrorMessage = "Sony Code cannot be longer than 100 characters.")]
    public string SonyCode { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(255, ErrorMessage = "Title cannot be longer than 255 characters.")]
    public string Title { get; set; }

    [StringLength(50)] public string? Region { get; set; }

    [Url] [StringLength(500)] public string? PosterUrl { get; set; }
}