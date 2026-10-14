using System.Linq.Expressions;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.Core.DataAccess.EntityFramework;

public class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
    where TEntity : class, IEntity, new()
    where TContext : DbContext
{
    protected readonly TContext Context;

    public EfEntityRepositoryBase(TContext context)
    {
        Context = context;
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await Context.Set<TEntity>().SingleOrDefaultAsync(filter);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        // Not: tüm satırları getirir; listeleme uçları için GetPagedAsync veya DAL detay sorgularını kullanın.
        return filter == null
            ? await Context.Set<TEntity>().ToListAsync()
            : await Context.Set<TEntity>().Where(filter).ToListAsync();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await Context.Set<TEntity>().AnyAsync(filter);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return filter == null
            ? await Context.Set<TEntity>().CountAsync()
            : await Context.Set<TEntity>().CountAsync(filter);
    }

    public Task<PagedResult<TResult>> GetPagedAsync<TResult>(
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector,
        PaginationParams paging)
    {
        var query = Context.Set<TEntity>().AsNoTracking();
        if (filter != null) query = query.Where(filter);
        return orderBy(query).Select(selector).ToPagedResultAsync(paging);
    }

    public async Task AddAsync(TEntity entity)
    {
        var addedEntity = Context.Entry(entity);
        addedEntity.State = EntityState.Added;
        await Context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        var updatedEntity = Context.Entry(entity);
        updatedEntity.State = EntityState.Modified;
        await Context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        // DbContext kaydederken Deleted durumunu soft delete'e çevirir.
        var deletedEntity = Context.Entry(entity);
        deletedEntity.State = EntityState.Deleted;
        await Context.SaveChangesAsync();
    }

    public void Add(TEntity entity) => Context.Entry(entity).State = EntityState.Added;

    public void Update(TEntity entity) => Context.Entry(entity).State = EntityState.Modified;

    public void Remove(TEntity entity) => Context.Entry(entity).State = EntityState.Deleted;
}
