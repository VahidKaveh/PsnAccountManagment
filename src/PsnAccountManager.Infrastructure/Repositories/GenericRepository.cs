using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;
using System.Linq.Expressions;

namespace PsnAccountManager.Infrastructure.Repositories;

public class GenericRepository<T, TId>(PsnAccountManagerDbContext context) : IGenericRepository<T, TId> where T : class
{
    protected readonly PsnAccountManagerDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(TId id) => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await DbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression) =>
        await DbSet.Where(expression).ToListAsync();

    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    public async Task<int> SaveChangesAsync() => await Context.SaveChangesAsync();
}