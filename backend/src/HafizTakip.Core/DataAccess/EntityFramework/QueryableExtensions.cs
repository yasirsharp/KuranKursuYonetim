using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.Core.DataAccess.EntityFramework;

public static class QueryableExtensions
{
    /// <summary>
    /// Sorguyu veritabanında sayfalar: önce toplam kayıt sayısı (COUNT), ardından yalnızca
    /// istenen sayfa (OFFSET/LIMIT) çekilir. Sorgu çağrılmadan önce sıralanmış olmalıdır.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PaginationParams paging)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<T>(items, totalCount, paging.Page, paging.PageSize);
    }

    /// <summary>
    /// Veritabanında sayfalar, ardından sayfadaki satırları bellekte dönüştürür
    /// (SQL'e çevrilemeyen eşlemeler için, ör. JSON alanlarının çözülmesi).
    /// </summary>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TRow, TResult>(
        this IQueryable<TRow> query, PaginationParams paging, Func<TRow, TResult> map)
    {
        var page = await query.ToPagedResultAsync(paging);
        return new PagedResult<TResult>(page.Data.Select(map).ToList(), page.TotalCount, page.Page, page.PageSize);
    }
}
