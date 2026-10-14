using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Business.Concrete;

public class EtutNoteManager : IEtutNoteService
{
    private readonly IEtutNoteDal _etutNoteDal;
    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;
    private readonly ICurrentUser _currentUser;
    private readonly IStudentAccessScope _accessScope;

    public EtutNoteManager(IEtutNoteDal etutNoteDal, IStudentDal studentDal, IUserDal userDal, ITenantContext tenantContext, ITenantReferenceValidator references, ICurrentUser currentUser, IStudentAccessScope accessScope)
    {
        _etutNoteDal = etutNoteDal;
        _studentDal = studentDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _references = references;
        _accessScope = accessScope;
    }

    public async Task<IDataResult<PagedResult<EtutNoteDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var notes = scopeIds == null
            ? await _etutNoteDal.GetListAsync()
            : await _etutNoteDal.GetListAsync(n => scopeIds.Contains(n.StudentId));
        
        var totalCount = notes.Count;
        var pagedList = notes
            .OrderByDescending(x => x.NoteDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<EtutNoteDto>();
        foreach (var note in pagedList)
        {
            var student = await _studentDal.GetAsync(s => s.Id == note.StudentId);
            var teacher = await _userDal.GetAsync(u => u.Id == note.TeacherId);
            
            dtoList.Add(new EtutNoteDto
            {
                Id = note.Id,
                TenantId = note.TenantId,
                StudentId = note.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                TeacherId = note.TeacherId,
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                NoteDate = note.NoteDate,
                HomeworkStatus = note.HomeworkStatus,
                TeacherNotes = note.TeacherNotes
            });
        }

        var result = new PagedResult<EtutNoteDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<EtutNoteDto>>(result, "Etüt notları listelendi.");
    }

    public async Task<IDataResult<PagedResult<EtutNoteDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<EtutNoteDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var notes = await _etutNoteDal.GetListAsync(n => n.StudentId == studentId);
        
        var totalCount = notes.Count;
        var pagedList = notes
            .OrderByDescending(x => x.NoteDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor";

        var dtoList = new List<EtutNoteDto>();
        foreach (var note in pagedList)
        {
            var teacher = await _userDal.GetAsync(u => u.Id == note.TeacherId);
            
            dtoList.Add(new EtutNoteDto
            {
                Id = note.Id,
                TenantId = note.TenantId,
                StudentId = note.StudentId,
                StudentName = studentName,
                TeacherId = note.TeacherId,
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                NoteDate = note.NoteDate,
                HomeworkStatus = note.HomeworkStatus,
                TeacherNotes = note.TeacherNotes
            });
        }

        var result = new PagedResult<EtutNoteDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<EtutNoteDto>>(result, "Öğrenci etüt notları listelendi.");
    }

    public async Task<IDataResult<EtutNoteDto>> GetByIdAsync(Guid id)
    {
        var note = await _etutNoteDal.GetAsync(n => n.Id == id);
        if (note == null || !await _accessScope.CanAccessStudentAsync(note.StudentId))
            return new ErrorDataResult<EtutNoteDto>("Etüt notu bulunamadı.", ResultErrorType.NotFound);

        var student = await _studentDal.GetAsync(s => s.Id == note.StudentId);
        var teacher = await _userDal.GetAsync(u => u.Id == note.TeacherId);

        var dto = new EtutNoteDto
        {
            Id = note.Id,
            TenantId = note.TenantId,
            StudentId = note.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            TeacherId = note.TeacherId,
            TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
            NoteDate = note.NoteDate,
            HomeworkStatus = note.HomeworkStatus,
            TeacherNotes = note.TeacherNotes
        };

        return new SuccessDataResult<EtutNoteDto>(dto, "Etüt notu getirildi.");
    }

    public async Task<IResult> AddAsync(CreateEtutNoteDto createEtutNoteDto)
    {
        var student = await _studentDal.GetAsync(s => s.Id == createEtutNoteDto.StudentId);
        if (student == null) return new ErrorResult("Öğrenci bulunamadı.", ResultErrorType.NotFound);

        // Hoca belirtilmediyse notu bırakan kullanıcıdır; belirtildiyse bu kursun hocası olmalıdır
        var teacherId = createEtutNoteDto.TeacherId is { } requestedTeacherId && requestedTeacherId != Guid.Empty
            ? requestedTeacherId
            : _currentUser.UserId ?? Guid.Empty;
        var teacherError = await _references.EnsureTeacherAsync(teacherId);
        if (teacherError != null) return teacherError;

        var note = new EtutNote
        {
            TenantId = _tenantContext.TenantId,
            StudentId = createEtutNoteDto.StudentId,
            TeacherId = teacherId,
            NoteDate = createEtutNoteDto.NoteDate.Date,
            HomeworkStatus = createEtutNoteDto.HomeworkStatus,
            TeacherNotes = createEtutNoteDto.TeacherNotes
        };

        await _etutNoteDal.AddAsync(note);
        return new SuccessResult("Etüt notu eklendi.");
    }

    public async Task<IResult> UpdateAsync(UpdateEtutNoteDto updateEtutNoteDto)
    {
        var note = await _etutNoteDal.GetAsync(n => n.Id == updateEtutNoteDto.Id);
        if (note == null) return new ErrorResult("Etüt notu bulunamadı.");

        note.HomeworkStatus = updateEtutNoteDto.HomeworkStatus;
        note.TeacherNotes = updateEtutNoteDto.TeacherNotes;
        note.UpdatedAt = DateTime.UtcNow;

        await _etutNoteDal.UpdateAsync(note);
        return new SuccessResult("Etüt notu güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var note = await _etutNoteDal.GetAsync(n => n.Id == id);
        if (note == null) return new ErrorResult("Etüt notu bulunamadı.");

        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;

        await _etutNoteDal.UpdateAsync(note);
        return new SuccessResult("Etüt notu silindi.");
    }
}
