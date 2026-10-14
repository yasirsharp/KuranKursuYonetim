using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface ILessonDal : IEntityRepository<Lesson>
{
    /// <summary>Öğrenci ve hoca adlarıyla, tarihe göre azalan, veritabanında sayfalanmış ders listesi.</summary>
    Task<PagedResult<LessonDto>> GetLessonDetailsPagedAsync(LessonFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<LessonDto?> GetLessonDetailAsync(Guid id);

    /// <summary>Öğrencinin en son dersleri (dashboard için).</summary>
    Task<List<LessonDto>> GetRecentLessonsAsync(Guid studentId, int count);
}
