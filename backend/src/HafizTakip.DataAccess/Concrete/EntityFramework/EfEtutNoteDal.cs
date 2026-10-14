using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfEtutNoteDal : EfEntityRepositoryBase<EtutNote, HafizTakipDbContext>, IEtutNoteDal
{
    public EfEtutNoteDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<EtutNoteDto>> GetEtutNoteDetailsPagedAsync(
        EtutNoteFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var notes = Context.EtutNotes.AsNoTracking();

        if (scopeStudentIds != null)
            notes = notes.Where(n => scopeStudentIds.Contains(n.StudentId));
        if (filter.StudentId.HasValue)
            notes = notes.Where(n => n.StudentId == filter.StudentId);

        return Details(notes.OrderByDescending(n => n.NoteDate).ThenByDescending(n => n.CreatedAt))
            .ToPagedResultAsync(paging);
    }

    public Task<EtutNoteDto?> GetEtutNoteDetailAsync(Guid id) =>
        Details(Context.EtutNotes.AsNoTracking().Where(n => n.Id == id)).FirstOrDefaultAsync();

    private IQueryable<EtutNoteDto> Details(IQueryable<EtutNote> notes) =>
        from n in notes
        join s in Context.Students on n.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        join u in Context.Users on n.TeacherId equals u.Id into teachers
        from u in teachers.DefaultIfEmpty()
        select new EtutNoteDto
        {
            Id = n.Id,
            TenantId = n.TenantId,
            StudentId = n.StudentId,
            StudentName = s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            TeacherId = n.TeacherId,
            TeacherName = u != null ? u.FirstName + " " + u.LastName : QueryHelpers.Unknown,
            NoteDate = n.NoteDate,
            HomeworkStatus = n.HomeworkStatus,
            TeacherNotes = n.TeacherNotes
        };
}
