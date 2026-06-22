using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations.
/// </summary>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Threshold above which a warning is logged.
    /// If a table exceeds this limit, consider pagination.
    /// </summary>
    protected const int LargeResultSetWarningThreshold = 500;

    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;
    private readonly ILogger<RepositoryBase<TEntity>> _logger;

    protected RepositoryBase(AppDbContext context, ILogger<RepositoryBase<TEntity>> logger)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
        _logger = logger;
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TEntity> result = await DbSet.ToListAsync(cancellationToken);

        // Warn if the dataset is large -- consider pagination.
        if (result.Count > LargeResultSetWarningThreshold)
        {
            _logger.LogWarning(
                "GetAllAsync returned {Count} records for {Entity}; consider adding pagination.",
                result.Count,
                typeof(TEntity).Name);
        }

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
