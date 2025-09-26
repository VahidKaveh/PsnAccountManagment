using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PsnAccountManager.Infrastructure.Repositories;

public class SettingRepository : GenericRepository<Setting, string>, ISettingRepository
{
    public SettingRepository(PsnAccountManagerDbContext context) : base(context) { }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue)
    {
        var setting = await _dbSet.FindAsync(key);
        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return defaultValue;
        }

        try
        {
            // This converter can handle enums, bools, ints, etc., making it very robust.
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                return (T)converter.ConvertFromString(setting.Value)!;
            }
            return defaultValue;
        }
        catch
        {
            // If the value in the DB is malformed, return the default value.
            return defaultValue;
        }
    }
}