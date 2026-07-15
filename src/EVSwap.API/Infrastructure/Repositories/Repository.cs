using EVSwap.API.Core.Interfaces.Repositories;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;

namespace EVSwap.API.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
