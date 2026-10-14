using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HafizTakip.IntegrationTests.Infrastructure;

/// <summary>EF'in veritabanına gönderdiği sorgu sayısını sayar (N+1 regresyon testleri için).</summary>
public sealed class CommandCounter : DbCommandInterceptor
{
    public int Count { get; private set; }

    public void Reset() => Count = 0;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
