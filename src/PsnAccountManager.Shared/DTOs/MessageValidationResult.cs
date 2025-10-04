using System.Collections.Generic;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Contains the result of a message validation operation.
/// </summary>
public class MessageValidationResult
{
    /// <summary>
    /// True if the message is considered valid, otherwise false.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// A list of issues or errors found during validation.
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// A score from 0.0 to 1.0 indicating the confidence in the validation result.
    /// </summary>
    public double ConfidenceScore { get; set; }
}