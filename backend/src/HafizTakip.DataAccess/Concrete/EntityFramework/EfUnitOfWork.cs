using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Results;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly HafizTakipDbContext _context;

    public EfUnitOfWork(HafizTakipDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> work) where TResult : IResult
    {
        // İç içe çağrılarda dıştaki transaction kullanılır
        if (_context.Database.CurrentTransaction != null)
        {
            return await work();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await work();
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return result;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            throw;
        }
    }
}
