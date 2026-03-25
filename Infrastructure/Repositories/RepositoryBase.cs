using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementazione base repository con operazioni CRUD comuni.
/// </summary>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Soglia oltre la quale viene emesso un warning in Debug.
    /// Se una tabella supera questo limite, considerare paginazione.
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
        var result = await DbSet.ToListAsync(cancellationToken);

        // Warning in Debug se il dataset è grande — considera paginazione
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
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Entity with Id {id} not found.");

        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
