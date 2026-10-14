using System.Linq.Expressions;
using HafizTakip.Core.Entities;

namespace HafizTakip.Core.DataAccess;

/// <summary>
/// Tüm veri erişim nesnelerinin (Repository) temel interface'idir.
/// </summary>
public interface IEntityRepository<T> where T : class, IEntity, new()
{
    Task<T?> GetAsync(Expression<Func<T, bool>> filter);
    Task<List<T>> GetListAsync(Expression<Func<T, bool>>? filter = null);

    /// <summary>Filtreye uyan kayıt var mı? (Global tenant/soft-delete filtreleri uygulanır.)</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
