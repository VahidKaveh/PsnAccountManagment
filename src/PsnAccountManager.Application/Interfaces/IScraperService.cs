using PsnAccountManager.Shared.DTOs;
namespace PsnAccountManager.Application.Interfaces;

/// <summary>
/// Defines the business logic for processing data scraped from external sources.
/// </summary>
public interface IScraperService
{
    /// <summary>
    /// Processes a single piece of parsed data, deciding whether to insert a new account,
    /// update an existing one, or mark an account as deleted.
    /// </summary>
    /// <param name="channelId">The ID of the channel from which the data was scraped.</param>
    /// <param name="parsedData">
    /// The structured data extracted from a message. 
    /// This parameter IS NULLABLE, as the parser might fail or the message might be irrelevant.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessScrapedDataAsync(int channelId, ParsedAccountDto? parsedData); //
}