using System.Diagnostics;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations.
/// </summary>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Threshold above which a Debug warning is emitted.
    /// If a table exceeds this limit, consider pagination.
    /// </summary>
    protected const int LargeResultSetWarningThreshold = 500;

    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected RepositoryBase(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TEntity> result = await DbSet.ToListAsync(cancellationToken);

        // Debug warning if the dataset is large — consider pagination
        Debug.WriteLineIf(result.Count > LargeResultSetWarningThreshold,
            $"[PERFORMANCE WARNING] {typeof(TEntity).Name}: GetAllAsync returned {result.Count} records. " +
            $"Consider adding pagination if this table continues to grow.");

        return result;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        TEntity entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"{typeof(TEntity).Name} with Id {id} not found.");

        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
