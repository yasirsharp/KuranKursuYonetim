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

    public async Task<IDataResult<PagedResult<AttendanceDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        // TODO: Veritabanında sayfala ve öğrenci adlarını toplu/projection sorgusuyla getir; mevcut akış tüm kayıtları yükleyip N+1 sorgu çalıştırıyor.
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var attendances = scopeIds == null
            ? await _attendanceDal.GetListAsync()
            : await _attendanceDal.GetListAsync(a => scopeIds.Contains(a.StudentId));
        
        var totalCount = attendances.Count;
        var pagedList = attendances
            .OrderByDescending(a => a.Date)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<AttendanceDto>();
        foreach (var a in pagedList)
        {
            var student = await _studentDal.GetAsync(s => s.Id == a.StudentId);
            dtoList.Add(new AttendanceDto
            {
                Id = a.Id,
                TenantId = a.TenantId,
                StudentId = a.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                Date = a.Date,
                Status = a.Status,
                Notes = a.Notes
            });
        }

        var result = new PagedResult<AttendanceDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<AttendanceDto>>(result, "Yoklama listesi getirildi.");
    }

    public async Task<IDataResult<PagedResult<AttendanceDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<AttendanceDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var attendances = await _attendanceDal.GetListAsync(a => a.StudentId == studentId);
        
        var totalCount = attendances.Count;
        var pagedList = attendances
            .OrderByDescending(a => a.Date)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<AttendanceDto>();
        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor";

        foreach (var a in pagedList)
        {
            dtoList.Add(new AttendanceDto
            {
                Id = a.Id,
                TenantId = a.TenantId,
                StudentId = a.StudentId,
                StudentName = studentName,
                Date = a.Date,
                Status = a.Status,
                Notes = a.Notes
            });
        }

        var result = new PagedResult<AttendanceDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<AttendanceDto>>(result, "Öğrenci yoklama listesi getirildi.");
    }

    public async Task<IDataResult<AttendanceDto>> GetByIdAsync(Guid id)
    {
        var a = await _attendanceDal.GetAsync(x => x.Id == id);
        if (a == null || !await _accessScope.CanAccessStudentAsync(a.StudentId))
            return new ErrorDataResult<AttendanceDto>("Yoklama kaydı bulunamadı.", ResultErrorType.NotFound);

        var student = await _studentDal.GetAsync(s => s.Id == a.StudentId);
        
        var dto = new AttendanceDto
        {
            Id = a.Id,
            TenantId = a.TenantId,
            StudentId = a.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            Date = a.Date,
            Status = a.Status,
            Notes = a.Notes
        };

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
