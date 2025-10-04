using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Shared.ViewModels;

// ViewModel for a single rule in the form
public class ParsingProfileRuleViewModel
{
    public int Id { get; set; }
    public int ParsingProfileId { get; set; }
    public ParsedFieldType FieldType { get; set; }

    [Required(ErrorMessage = "Regex Pattern is required.")]
    public string RegexPattern { get; set; }
}

// Main ViewModel for the entire Edit page
public class ParsingProfileEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Profile Name")]
    public string Name { get; set; }

    // We use the specific ViewModel for the rules
    public List<ParsingProfileRuleViewModel> Rules { get; set; } = new();
}