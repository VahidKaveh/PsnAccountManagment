using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces;

public interface IGameService
{
    Task<GameDto?> GetByIdAsync(int id);
    Task<IEnumerable<GameDto>> GetAllAsync();
    Task<IEnumerable<GameDto>> SearchByTitleAsync(string titleQuery);
    Task<GameDto> CreateAsync(CreateGameDto createGameDto);
    Task<bool> UpdateAsync(int id, UpdateGameDto updateGameDto);
    Task<bool> DeleteAsync(int id);
}