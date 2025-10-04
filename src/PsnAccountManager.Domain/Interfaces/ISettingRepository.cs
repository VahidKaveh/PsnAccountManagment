using System.Threading.Tasks;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

// Note: The primary key for Setting is a string (the key itself)
public interface ISettingRepository : IGenericRepository<Setting, string>
{
    /// <summary>
    /// Gets a setting value by its key, converts it to the specified type,
    /// and returns a default value if not found.
    /// </summary>
    Task<T> GetValueAsync<T>(string key, T defaultValue);
}