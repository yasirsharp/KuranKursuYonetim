using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfQuranDal : IQuranDal
{
    private readonly HafizTakipDbContext _context;

    public EfQuranDal(HafizTakipDbContext context)
    {
        _context = context;
    }

    public Task<List<QuranSurah>> GetSurahsAsync() =>
        _context.QuranSurahs.AsNoTracking().OrderBy(s => s.Id).ToListAsync();

    public Task<List<QuranPage>> GetPagesAsync() =>
        _context.QuranPages.AsNoTracking().OrderBy(p => p.Page).ToListAsync();
}
