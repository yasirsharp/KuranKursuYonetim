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

public class AttendanceManager : IAttendanceService
{
    private readonly IAttendanceDal _attendanceDal;
    private readonly IStudentDal _studentDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;
    private readonly IStudentAccessScope _accessScope;
    private readonly IUnitOfWork _unitOfWork;

    public AttendanceManager(IAttendanceDal attendanceDal, IStudentDal studentDal, ITenantContext tenantContext, ITenantReferenceValidator references, IStudentAccessScope accessScope, IUnitOfWork unitOfWork)
    {
        _attendanceDal = attendanceDal;
        _studentDal = studentDal;
        _tenantContext = tenantContext;
        _references = references;
        _accessScope = accessScope;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<PagedResult<AttendanceDto>>> GetAllAsync(AttendanceFilter filter, PaginationParams paginationParams)
    {
        if (filter.StudentId is { } studentId && !await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<AttendanceDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var result = await _attendanceDal.GetAttendanceDetailsPagedAsync(filter, scopeIds, paginationParams);
        return new SuccessDataResult<PagedResult<AttendanceDto>>(result, "Yoklama kayıtları listelendi.");
    }

    public async Task<IDataResult<PagedResult<AttendanceDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<AttendanceDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var result = await _attendanceDal.GetAttendanceDetailsPagedAsync(new AttendanceFilter { StudentId = studentId }, null, paginationParams);
        return new SuccessDataResult<PagedResult<AttendanceDto>>(result, "Öğrencinin yoklama geçmişi listelendi.");
    }

    public async Task<IDataResult<AttendanceDto>> GetByIdAsync(Guid id)
    {
        var dto = await _attendanceDal.GetAttendanceDetailAsync(id);
        if (dto == null || !await _accessScope.CanAccessStudentAsync(dto.StudentId))
            return new ErrorDataResult<AttendanceDto>("Yoklama kaydı bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<AttendanceDto>(dto, "Yoklama detayı getirildi.");
    }

    [ValidationAspect(typeof(CreateAttendanceValidator))]
    public async Task<IResult> AddAsync(CreateAttendanceDto createAttendanceDto)
    {
        var referenceError = await _references.EnsureStudentAsync(createAttendanceDto.StudentId);
        if (referenceError != null) return referenceError;

        var dateUtc = DateTime.SpecifyKind(createAttendanceDto.Date.Date, DateTimeKind.Utc);
        var nextDateUtc = dateUtc.AddDays(1);
        var existing = await _attendanceDal.GetAsync(a => a.StudentId == createAttendanceDto.StudentId && a.Date >= dateUtc && a.Date < nextDateUtc);
        if (existing != null)
        {
            return new ErrorResult("Bu öğrenci için bu tarihte zaten bir yoklama kaydı var.", ResultErrorType.Conflict);
        }

        var attendance = new Attendance
        {
            TenantId = _tenantContext.TenantId,
            StudentId = createAttendanceDto.StudentId,
            Date = dateUtc,
            Status = createAttendanceDto.Status,
            Notes = createAttendanceDto.Notes
        };

        await _attendanceDal.AddAsync(attendance);
        return new SuccessResult("Yoklama kaydedildi.");
    }

    [ValidationAspect(typeof(BulkAttendanceValidator))]
    public async Task<IResult> AddBulkAsync(BulkAttendanceDto bulkAttendanceDto)
    {
        var referenceError = await _references.EnsureStudentsAsync(
            bulkAttendanceDto.Attendances.Select(a => a.StudentId).ToList());
        if (referenceError != null) return referenceError;

        var dateUtc = DateTime.SpecifyKind(bulkAttendanceDto.Date.Date, DateTimeKind.Utc);
        var nextDateUtc = dateUtc.AddDays(1);

        // Aynı öğrenci listede birden fazla geçerse son değer geçerlidir
        var items = bulkAttendanceDto.Attendances
            .GroupBy(a => a.StudentId)
            .Select(g => g.Last())
            .ToList();
        var studentIds = items.Select(a => a.StudentId).ToList();

        // O günün mevcut kayıtları tek sorguda alınır
        var existingByStudent = (await _attendanceDal.GetListAsync(a =>
                studentIds.Contains(a.StudentId) && a.Date >= dateUtc && a.Date < nextDateUtc))
            .ToDictionary(a => a.StudentId);

        foreach (var item in items)
        {
            if (existingByStudent.TryGetValue(item.StudentId, out var existing))
            {
                // Varsa güncelle
                existing.Status = item.Status;
                existing.Notes = item.Notes;
                _attendanceDal.Update(existing);
            }
            else
            {
                // Yoksa ekle
                _attendanceDal.Add(new Attendance
                {
                    TenantId = _tenantContext.TenantId,
                    StudentId = item.StudentId,
                    Date = dateUtc,
                    Status = item.Status,
                    Notes = item.Notes
                });
            }
        }

        // Tüm sınıfın yoklaması tek SaveChanges ile atomik kaydedilir
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Toplu yoklama işlemi başarıyla tamamlandı.");
    }

    [ValidationAspect(typeof(UpdateAttendanceValidator))]
    public async Task<IResult> UpdateAsync(AttendanceDto updateAttendanceDto)
    {
        var attendance = await _attendanceDal.GetAsync(a => a.Id == updateAttendanceDto.Id);
        if (attendance == null) return new ErrorResult("Yoklama kaydı bulunamadı.");

        attendance.Status = updateAttendanceDto.Status;
        attendance.Notes = updateAttendanceDto.Notes;
        attendance.UpdatedAt = DateTime.UtcNow;

        await _attendanceDal.UpdateAsync(attendance);
        return new SuccessResult("Yoklama güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var attendance = await _attendanceDal.GetAsync(a => a.Id == id);
        if (attendance == null) return new ErrorResult("Yoklama kaydı bulunamadı.");

        attendance.IsDeleted = true;
        attendance.DeletedAt = DateTime.UtcNow;
        await _attendanceDal.UpdateAsync(attendance);
        
        return new SuccessResult("Yoklama silindi.");
    }
}
