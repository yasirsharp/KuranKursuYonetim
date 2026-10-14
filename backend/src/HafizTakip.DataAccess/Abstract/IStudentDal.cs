using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IStudentDal : IEntityRepository<Student>
{
    /// <summary>Grup adıyla birlikte, veritabanında filtrelenmiş ve sayfalanmış öğrenci listesi.</summary>
    /// <param name="scopeStudentIds">Veli kapsamı: doluysa yalnızca bu öğrenciler (null = kısıtsız).</param>
    Task<PagedResult<StudentDto>> GetStudentDetailsPagedAsync(StudentFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<StudentDto?> GetStudentDetailAsync(Guid id);

    /// <summary>Tamamlanan sayfa ortalaması (veritabanında AVG); öğrenci yoksa 0. groupId verilirse yalnızca o sınıf.</summary>
    Task<double> GetAverageCompletedPagesAsync(Guid? groupId = null);
}
