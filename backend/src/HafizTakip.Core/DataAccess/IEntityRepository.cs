using System.Linq.Expressions;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;

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

    /// <summary>
    /// Veritabanında filtreler, sıralar, projeksiyon yapar ve sayfalar (ilişkisiz basit listeler için).
    /// İlişkili tablolardan ad vb. gereken listeler için ilgili DAL'daki detay sorgusunu kullanın.
    /// </summary>
    Task<PagedResult<TResult>> GetPagedAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, TResult>> selector,
        PaginationParams paging);

    // Hemen kaydeden metotlar (tek kayıtlık basit işlemler için)
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);

    // Kaydetmeden işaretleyen metotlar: birden fazla değişiklik IUnitOfWork ile tek seferde kaydedilir
    void Add(T entity);
    void Update(T entity);

    /// <summary>Silme olarak işaretler; kaydedilirken soft delete'e çevrilir.</summary>
    void Remove(T entity);
}
