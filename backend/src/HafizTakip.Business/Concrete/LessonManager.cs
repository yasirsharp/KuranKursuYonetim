using HafizTakip.Core.Enums;
using HafizTakip.Core.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class LessonManager : ILessonService
{
    private readonly ILessonDal _lessonDal;
    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;
    private readonly ICurrentUser _currentUser;
    private readonly IStudentAccessScope _accessScope;
    private readonly IUnitOfWork _unitOfWork;

    public LessonManager(ILessonDal lessonDal, IStudentDal studentDal, IUserDal userDal, ITenantContext tenantContext, ITenantReferenceValidator references, ICurrentUser currentUser, IStudentAccessScope accessScope, IUnitOfWork unitOfWork)
    {
        _lessonDal = lessonDal;
        _studentDal = studentDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _references = references;
        _accessScope = accessScope;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<PagedResult<LessonDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        // TODO: Sayfalama/projection ile birlikte öğrenci ve öğretmen adlarını tek sorguda getir; mevcut döngü N+1 sorgu üretir.
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var lessons = scopeIds == null
            ? await _lessonDal.GetListAsync()
            : await _lessonDal.GetListAsync(l => scopeIds.Contains(l.StudentId));
        
        var totalCount = lessons.Count;
        var pagedLessons = lessons
            .OrderByDescending(l => l.LessonDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<LessonDto>();
        foreach (var l in pagedLessons)
        {
            var student = await _studentDal.GetAsync(s => s.Id == l.StudentId);
            var teacher = await _userDal.GetAsync(u => u.Id == l.TeacherId);

            dtoList.Add(new LessonDto
            {
                Id = l.Id,
                TenantId = l.TenantId,
                StudentId = l.StudentId,
                TeacherId = l.TeacherId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                LessonDate = l.LessonDate,
                CuzNo = l.JuzNumber,
                PageNo = l.PageNumber,
                SurahName = l.SuraId.ToString(),
                IsNewPage = l.IsNewPage,
                Performance = l.Performance,
                HomeworkStatus = l.HomeworkStatus,
                TeacherNotes = l.Notes
            });
        }

        var result = new PagedResult<LessonDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<LessonDto>>(result, "Dersler listelendi.");
    }

    public async Task<IDataResult<PagedResult<LessonDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<LessonDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var lessons = await _lessonDal.GetListAsync(l => l.StudentId == studentId);
        
        var totalCount = lessons.Count;
        var pagedLessons = lessons
            .OrderByDescending(l => l.LessonDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<LessonDto>();
        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        
        foreach (var l in pagedLessons)
        {
            var teacher = await _userDal.GetAsync(u => u.Id == l.TeacherId);

            dtoList.Add(new LessonDto
            {
                Id = l.Id,
                TenantId = l.TenantId,
                StudentId = l.StudentId,
                TeacherId = l.TeacherId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                LessonDate = l.LessonDate,
                CuzNo = l.JuzNumber,
                PageNo = l.PageNumber,
                SurahName = l.SuraId.ToString(),
                IsNewPage = l.IsNewPage,
                Performance = l.Performance,
                HomeworkStatus = l.HomeworkStatus,
                TeacherNotes = l.Notes
            });
        }

        var result = new PagedResult<LessonDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<LessonDto>>(result, "Öğrenci dersleri listelendi.");
    }

    public async Task<IDataResult<LessonDto>> GetByIdAsync(Guid id)
    {
        var l = await _lessonDal.GetAsync(x => x.Id == id);
        if (l == null || !await _accessScope.CanAccessStudentAsync(l.StudentId))
            return new ErrorDataResult<LessonDto>("Ders bulunamadı.", ResultErrorType.NotFound);

        var student = await _studentDal.GetAsync(s => s.Id == l.StudentId);
        var teacher = await _userDal.GetAsync(u => u.Id == l.TeacherId);

        var dto = new LessonDto
        {
            Id = l.Id,
            TenantId = l.TenantId,
            StudentId = l.StudentId,
            TeacherId = l.TeacherId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
            LessonDate = l.LessonDate,
            CuzNo = l.JuzNumber,
            PageNo = l.PageNumber,
            SurahName = l.SuraId.ToString(),
            IsNewPage = l.IsNewPage,
            Performance = l.Performance,
            HomeworkStatus = l.HomeworkStatus,
            TeacherNotes = l.Notes
        };

        return new SuccessDataResult<LessonDto>(dto, "Ders getirildi.");
    }

    [ValidationAspect(typeof(CreateLessonValidator))]
    public async Task<IResult> AddAsync(CreateLessonDto createLessonDto)
    {
        // 1. Aynı öğrenciye, aynı tarihte ders var mı kontrolü
        var lessonDateUtc = DateTime.SpecifyKind(createLessonDto.LessonDate.Date, DateTimeKind.Utc);
        var nextDateUtc = lessonDateUtc.AddDays(1);
        var existingLesson = await _lessonDal.GetAsync(l => l.StudentId == createLessonDto.StudentId && l.LessonDate >= lessonDateUtc && l.LessonDate < nextDateUtc);
        if (existingLesson != null)
        {
            return new ErrorResult("Bu öğrenci için bu tarihte zaten bir ders kaydı mevcut.");
        }

        var student = await _studentDal.GetAsync(s => s.Id == createLessonDto.StudentId);
        if (student == null) return new ErrorResult("Öğrenci bulunamadı.", ResultErrorType.NotFound);

        // Hoca belirtilmediyse dersi kaydeden kullanıcıdır; belirtildiyse bu kursun hocası olmalıdır
        var teacherId = createLessonDto.TeacherId is { } requestedTeacherId && requestedTeacherId != Guid.Empty
            ? requestedTeacherId
            : _currentUser.UserId ?? Guid.Empty;
        var teacherError = await _references.EnsureTeacherAsync(teacherId);
        if (teacherError != null) return teacherError;

        var lesson = new Lesson
        {
            TenantId = _tenantContext.TenantId,
            StudentId = createLessonDto.StudentId,
            TeacherId = teacherId,
            LessonDate = lessonDateUtc,
            JuzNumber = createLessonDto.CuzNo ?? 0,
            PageNumber = createLessonDto.PageNo ?? 0,
            // TODO: SurahName'i doğrulanmış sure kimliğine eşle; şu an gelen sure bilgisi kayboluyor.
            SuraId = 0,
            IsNewPage = createLessonDto.IsNewPage,
            Performance = createLessonDto.Performance,
            HomeworkStatus = createLessonDto.HomeworkStatus,
            Notes = createLessonDto.TeacherNotes
        };

        // Eğer yeni sayfa verildiyse (Tekrar değilse), öğrencinin ilerlemesini güncelle
        if (createLessonDto.IsNewPage)
        {
            // TODO: Sayfa aralığı/tekrarları dikkate alan idempotent ilerleme hesabı (Sprint 6 — ezber haritası).
            student.CompletedPages += 1;
            student.CurrentPage = createLessonDto.PageNo ?? student.CurrentPage;

            // Hafızlık bitiş kontrolü (toplam 604 sayfa)
            if (student.CompletedPages >= 604)
            {
                student.IsHafiz = true;
                student.Status = StudentStatus.Graduated;
            }

            _studentDal.Update(student);
        }

        _lessonDal.Add(lesson);

        // Ders ve öğrenci ilerlemesi tek SaveChanges ile atomik kaydedilir: biri başarısız olursa ikisi de yazılmaz
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Ders başarıyla kaydedildi.");
    }

    [ValidationAspect(typeof(UpdateLessonValidator))]
    public async Task<IResult> UpdateAsync(UpdateLessonDto updateLessonDto)
    {
        var lesson = await _lessonDal.GetAsync(x => x.Id == updateLessonDto.Id);
        if (lesson == null) return new ErrorResult("Ders bulunamadı.");

        lesson.JuzNumber = updateLessonDto.CuzNo ?? lesson.JuzNumber;
        lesson.PageNumber = updateLessonDto.PageNo ?? lesson.PageNumber;
        lesson.SuraId = 0; // updateLessonDto.SurahName varsa int parse edilebilir
        lesson.Performance = updateLessonDto.Performance;
        lesson.HomeworkStatus = updateLessonDto.HomeworkStatus;
        lesson.Notes = updateLessonDto.TeacherNotes;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _lessonDal.UpdateAsync(lesson);
        return new SuccessResult("Ders başarıyla güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var lesson = await _lessonDal.GetAsync(x => x.Id == id);
        if (lesson == null) return new ErrorResult("Ders bulunamadı.");

        lesson.IsDeleted = true;
        lesson.DeletedAt = DateTime.UtcNow;
        await _lessonDal.UpdateAsync(lesson);
        
        return new SuccessResult("Ders silindi.");
    }
}
