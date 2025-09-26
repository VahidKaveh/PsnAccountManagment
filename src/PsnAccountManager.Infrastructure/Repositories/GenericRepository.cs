using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Linq.Expressions;

namespace PsnAccountManager.Infrastructure.Repositories;

public class GenericRepository<T, TId> : IGenericRepository<T, TId> where T : class
{
    protected readonly PsnAccountManagerDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(PsnAccountManagerDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(TId id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression) =>
        await _dbSet.Where(expression).ToListAsync();

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}