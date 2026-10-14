using HafizTakip.Core.Constants;
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
    private readonly IQuranReferenceService _quran;

    public LessonManager(ILessonDal lessonDal, IStudentDal studentDal, IUserDal userDal, ITenantContext tenantContext, ITenantReferenceValidator references, ICurrentUser currentUser, IStudentAccessScope accessScope, IUnitOfWork unitOfWork, IQuranReferenceService quran)
    {
        _lessonDal = lessonDal;
        _studentDal = studentDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _references = references;
        _accessScope = accessScope;
        _unitOfWork = unitOfWork;
        _quran = quran;
    }

    public async Task<IDataResult<PagedResult<LessonDto>>> GetAllAsync(LessonFilter filter, PaginationParams paginationParams)
    {
        if (filter.StudentId is { } studentId && !await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<LessonDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var result = await _lessonDal.GetLessonDetailsPagedAsync(filter, scopeIds, paginationParams);
        return new SuccessDataResult<PagedResult<LessonDto>>(result, "Dersler listelendi.");
    }

    public async Task<IDataResult<PagedResult<LessonDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<LessonDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var result = await _lessonDal.GetLessonDetailsPagedAsync(new LessonFilter { StudentId = studentId }, null, paginationParams);
        return new SuccessDataResult<PagedResult<LessonDto>>(result, "Öğrenci dersleri listelendi.");
    }

    public async Task<IDataResult<LessonDto>> GetByIdAsync(Guid id)
    {
        var dto = await _lessonDal.GetLessonDetailAsync(id);
        if (dto == null || !await _accessScope.CanAccessStudentAsync(dto.StudentId))
            return new ErrorDataResult<LessonDto>("Ders bulunamadı.", ResultErrorType.NotFound);

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

        var page = createLessonDto.PageNo!.Value; // validator sayfayı zorunlu ve 1-604 tutar
        var lesson = new Lesson
        {
            TenantId = _tenantContext.TenantId,
            StudentId = createLessonDto.StudentId,
            TeacherId = teacherId,
            LessonDate = lessonDateUtc,
            // Cüz ve sure istemciden alınmaz, sayfadan türetilir (ayetberkenar mushaf)
            JuzNumber = QuranConstants.JuzOfPage(page),
            PageNumber = page,
            SuraId = await _quran.GetFirstSurahIdOfPageAsync(page) ?? 0,
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

        if (updateLessonDto.PageNo is { } newPage)
        {
            lesson.PageNumber = newPage;
            lesson.JuzNumber = QuranConstants.JuzOfPage(newPage);
            lesson.SuraId = await _quran.GetFirstSurahIdOfPageAsync(newPage) ?? 0;
        }
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
