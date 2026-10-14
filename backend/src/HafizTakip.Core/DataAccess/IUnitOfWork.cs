using HafizTakip.Core.Utilities.Results;

namespace HafizTakip.Core.DataAccess;

/// <summary>
/// Bir istekteki tüm repository değişikliklerini tek seferde (atomik olarak) kaydeder.
/// Repository'ler istek başına aynı DbContext'i paylaştığı için Add/Update/Remove ile
/// işaretlenen değişiklikler burada birlikte kaydedilir.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();

    /// <summary>
    /// İşi bir veritabanı transaction'ı içinde çalıştırır ve sonunda bekleyen değişiklikleri kaydeder.
    /// İş başarısız sonuç dönerse (Success == false) veya hata fırlatırsa hiçbir değişiklik kalıcı olmaz.
    /// İş içinde hemen kaydeden eski metotlar (AddAsync vb.) da aynı transaction'a dahildir.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> work) where TResult : IResult;
}
