using Microsoft.Extensions.Logging;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Application.Interfaces;

namespace PsnAccountManager.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GameService> _logger;
    // In a real-world project, you would inject an IMapper (like AutoMapper)
    // private readonly IMapper _mapper;

    public GameService(IGameRepository gameRepository, ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<GameDto?> GetByIdAsync(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);
        if (game == null)
        {
            _logger.LogWarning("Game with ID {GameId} not found.", id);
            return null;
        }
        // Manual mapping (replace with AutoMapper)
        return MapToGameDto(game);
    }

    public async Task<IEnumerable<GameDto>> GetAllAsync()
    {
        var games = await _gameRepository.GetAllAsync();
        // Manual mapping for a collection
        return games.Select(MapToGameDto);
    }

    public async Task<IEnumerable<GameDto>> SearchByTitleAsync(string titleQuery)
    {
        var games = await _gameRepository.SearchByTitleAsync(titleQuery);
        return games.Select(MapToGameDto);
    }

    public async Task<GameDto> CreateAsync(CreateGameDto createGameDto)
    {
        // --- Business Logic: Ensure SonyCode is unique ---
        var existingGame = await _gameRepository.GetBySonyCodeAsync(createGameDto.SonyCode);
        if (existingGame != null)
        {
            throw new InvalidOperationException($"A game with SonyCode '{createGameDto.SonyCode}' already exists.");
        }

        // Map DTO to Entity
        var newGame = new Game
        {
            SonyCode = createGameDto.SonyCode,
            Title = createGameDto.Title,
            Region = createGameDto.Region,
            PosterUrl = createGameDto.PosterUrl
        };

        await _gameRepository.AddAsync(newGame);
        await _gameRepository.SaveChangesAsync();

        _logger.LogInformation("New game created with ID {GameId} and SonyCode {SonyCode}", newGame.Id, newGame.SonyCode);

        // Map the newly created entity back to a DTO to return it
        return MapToGameDto(newGame);
    }

    public async Task<bool> UpdateAsync(int id, UpdateGameDto updateGameDto)
    {
        var existingGame = await _gameRepository.GetByIdAsync(id);
        if (existingGame == null)
        {
            _logger.LogWarning("Update failed: Game with ID {GameId} not found.", id);
            return false;
        }

        // Update properties
        existingGame.Title = updateGameDto.Title;
        existingGame.Region = updateGameDto.Region;
        existingGame.PosterUrl = updateGameDto.PosterUrl;

        _gameRepository.Update(existingGame);
        await _gameRepository.SaveChangesAsync();

        _logger.LogInformation("Game with ID {GameId} was updated.", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var gameToDelete = await _gameRepository.GetByIdAsync(id);
        if (gameToDelete == null)
        {
            _logger.LogWarning("Delete failed: Game with ID {GameId} not found.", id);
            return false;
        }

        _gameRepository.Remove(gameToDelete);
        await _gameRepository.SaveChangesAsync();

        _logger.LogInformation("Game with ID {GameId} was deleted.", id);
        return true;
    }

    // --- Private Helper Method for Mapping ---
    private GameDto MapToGameDto(Game game)
    {
        // This would be replaced by _mapper.Map<GameDto>(game)
        return new GameDto
        {
            Id = game.Id,
            SonyCode = game.SonyCode,
            Title = game.Title,
            Region = game.Region,
            PosterUrl = game.PosterUrl
        };
    }
}