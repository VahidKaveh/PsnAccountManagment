using System.Linq.Expressions;

namespace PsnAccountManager.Domain.Interfaces;

// T must be a class inheriting from our BaseEntity
public interface IGenericRepository<T, TId> where T : class
{
    Task<T?> GetByIdAsync(TId id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(); // Unit of Work implementation
}