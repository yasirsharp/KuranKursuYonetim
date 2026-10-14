using HafizTakip.Core.Enums;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.DataAccess;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Core.Utilities.IoC;
using System.Text.Json;

namespace HafizTakip.Business.Concrete;

public class DailyAssignmentManager : IDailyAssignmentService
{
    private readonly IDailyAssignmentDal _dailyAssignmentDal;
    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly IParentNotificationService _notificationService;
    private readonly ICurrentUser _currentUser;
    private readonly IStudentAccessScope _accessScope;
    private readonly IUnitOfWork _unitOfWork;

    public DailyAssignmentManager(
        IDailyAssignmentDal dailyAssignmentDal, 
        IStudentDal studentDal, 
        IUserDal userDal, 
        ITenantContext tenantContext,
        IParentNotificationService notificationService,
        ICurrentUser currentUser, IStudentAccessScope accessScope, IUnitOfWork unitOfWork)
    {
        _dailyAssignmentDal = dailyAssignmentDal;
        _studentDal = studentDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _accessScope = accessScope;
        _unitOfWork = unitOfWork;
    }

    private Guid GetCurrentUserId() => _currentUser.UserId ?? Guid.Empty;

    public async Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var assignments = await _dailyAssignmentDal.GetListAsync();
        return await CreatePagedResult(assignments, paginationParams);
    }

    public async Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<DailyAssignmentDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var assignments = await _dailyAssignmentDal.GetListAsync(a => a.StudentId == studentId);
        return await CreatePagedResult(assignments, paginationParams);
    }

    public async Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetEtutAssignmentsAsync(DateTime date, PaginationParams paginationParams)
    {
        var targetDateUtc = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var nextDateUtc = targetDateUtc.AddDays(1);
        var assignments = await _dailyAssignmentDal.GetListAsync(a => a.AssignmentDate >= targetDateUtc && a.AssignmentDate < nextDateUtc && a.IsBoarderToday && a.Status == AssignmentStatus.Assigned);
        return await CreatePagedResult(assignments, paginationParams);
    }

    public async Task<IDataResult<DailyAssignmentDto>> GetByIdAsync(Guid id)
    {
        var assignment = await _dailyAssignmentDal.GetAsync(a => a.Id == id);
        if (assignment == null || !await _accessScope.CanAccessStudentAsync(assignment.StudentId))
            return new ErrorDataResult<DailyAssignmentDto>("Görev bulunamadı.", ResultErrorType.NotFound);

        var student = await _studentDal.GetAsync(s => s.Id == assignment.StudentId);
        var teacher = await _userDal.GetAsync(t => t.Id == assignment.TeacherId);

        var dto = new DailyAssignmentDto
        {
            Id = assignment.Id,
            TenantId = assignment.TenantId,
            StudentId = assignment.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            TeacherId = assignment.TeacherId,
            TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
            AssignmentDate = assignment.AssignmentDate,
            IsBoarderToday = assignment.IsBoarderToday,
            AssignedTasks = !string.IsNullOrWhiteSpace(assignment.AssignedTasks) ? (JsonSerializer.Deserialize<List<AssignmentTaskItem>>(assignment.AssignedTasks) ?? new()) : new(),
            CompletedInEtut = !string.IsNullOrWhiteSpace(assignment.CompletedInEtut) ? (JsonSerializer.Deserialize<List<string>>(assignment.CompletedInEtut) ?? new()) : new(),
            CompletedAtHome = !string.IsNullOrWhiteSpace(assignment.CompletedAtHome) ? (JsonSerializer.Deserialize<List<string>>(assignment.CompletedAtHome) ?? new()) : new(),
            Status = assignment.Status,
            EtutTeacherNote = assignment.EtutTeacherNote,
            ParentNote = assignment.ParentNote
        };

        return new SuccessDataResult<DailyAssignmentDto>(dto);
    }

    [ValidationAspect(typeof(CreateDailyAssignmentValidator))]
    public async Task<Core.Utilities.Results.IResult> AddAsync(CreateDailyAssignmentDto dto)
    {
        var student = await _studentDal.GetAsync(s => s.Id == dto.StudentId);
        if (student == null) return new ErrorResult("Öğrenci bulunamadı.");

        bool isBoarder = dto.OverrideIsBoarder ?? student.IsBoarder;
        var teacherId = GetCurrentUserId();

        var assignment = new DailyAssignment
        {
            TenantId = _tenantContext.TenantId,
            StudentId = dto.StudentId,
            TeacherId = teacherId,
            AssignmentDate = dto.AssignmentDate,
            IsBoarderToday = isBoarder,
            AssignedTasks = JsonSerializer.Serialize(dto.AssignedTasks),
            Status = isBoarder ? AssignmentStatus.Assigned : AssignmentStatus.SentToParent
        };

        // Görev ve (gerekiyorsa) veli bildirimi birlikte kaydedilir; biri başarısız olursa ikisi de geri alınır
        return await _unitOfWork.ExecuteInTransactionAsync<Core.Utilities.Results.IResult>(async () =>
        {
            await _dailyAssignmentDal.AddAsync(assignment);

            if (!isBoarder)
            {
                // Direkt eve gidiyorsa, veliye bildirim at.
                var notificationResult = await SendParentNotification(student, assignment);
                if (!notificationResult.Success) return notificationResult;
            }

            return new SuccessResult("Görev başarıyla atandı.");
        });
    }

    public async Task<Core.Utilities.Results.IResult> CompleteEtutAsync(CompleteEtutAssignmentDto dto)
    {
        var assignment = await _dailyAssignmentDal.GetAsync(a => a.Id == dto.Id);
        if (assignment == null) return new ErrorResult("Görev bulunamadı.");
        if (assignment.Status != AssignmentStatus.Assigned) return new ErrorResult("Bu görev artık etüt hocasında değil.");

        assignment.CompletedInEtut = JsonSerializer.Serialize(dto.CompletedTaskIds);
        if (!string.IsNullOrEmpty(dto.EtutTeacherNote))
            assignment.EtutTeacherNote = dto.EtutTeacherNote;

        // Görev güncellemesi ve (gerekiyorsa) veli bildirimi birlikte kaydedilir
        return await _unitOfWork.ExecuteInTransactionAsync<Core.Utilities.Results.IResult>(async () =>
        {
            if (dto.FinishEtut)
            {
                // Kalanları kontrol et
                var assignedTasks = JsonSerializer.Deserialize<List<AssignmentTaskItem>>(assignment.AssignedTasks) ?? new();
                var uncompletedCount = assignedTasks.Count(t => !dto.CompletedTaskIds.Contains(t.Id));

                if (uncompletedCount > 0)
                {
                    assignment.Status = AssignmentStatus.SentToParent;
                    var student = await _studentDal.GetAsync(s => s.Id == assignment.StudentId);
                    if (student != null)
                    {
                        var notificationResult = await SendParentNotification(student, assignment);
                        if (!notificationResult.Success) return notificationResult;
                    }
                }
                else
                {
                    assignment.Status = AssignmentStatus.Completed;
                }
            }

            await _dailyAssignmentDal.UpdateAsync(assignment);
            return new SuccessResult("Etüt değerlendirmesi kaydedildi.");
        });
    }

    public async Task<Core.Utilities.Results.IResult> CompleteHomeAsync(CompleteHomeAssignmentDto dto)
    {
        var assignment = await _dailyAssignmentDal.GetAsync(a => a.Id == dto.Id);
        if (assignment == null || !await _accessScope.CanAccessStudentAsync(assignment.StudentId))
            return new ErrorResult("Görev bulunamadı.", ResultErrorType.NotFound);
        if (assignment.Status != AssignmentStatus.SentToParent) return new ErrorResult("Bu görev şu an veli onayında değil.");

        assignment.CompletedAtHome = JsonSerializer.Serialize(dto.CompletedTaskIds);
        if (!string.IsNullOrEmpty(dto.ParentNote))
            assignment.ParentNote = dto.ParentNote;

        assignment.Status = AssignmentStatus.Completed;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _dailyAssignmentDal.UpdateAsync(assignment);
        return new SuccessResult("Ev ödevi tamamlandı olarak işaretlendi.");
    }

    private async Task<IDataResult<PagedResult<DailyAssignmentDto>>> CreatePagedResult(IList<DailyAssignment> assignments, PaginationParams paginationParams)
    {
        var totalCount = assignments.Count;
        var pagedList = assignments
            .OrderByDescending(x => x.AssignmentDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<DailyAssignmentDto>();
        foreach (var assignment in pagedList)
        {
            var student = await _studentDal.GetAsync(s => s.Id == assignment.StudentId);
            var teacher = await _userDal.GetAsync(t => t.Id == assignment.TeacherId);

            dtoList.Add(new DailyAssignmentDto
            {
                Id = assignment.Id,
                TenantId = assignment.TenantId,
                StudentId = assignment.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                TeacherId = assignment.TeacherId,
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                AssignmentDate = assignment.AssignmentDate,
                IsBoarderToday = assignment.IsBoarderToday,
                AssignedTasks = !string.IsNullOrWhiteSpace(assignment.AssignedTasks) ? (JsonSerializer.Deserialize<List<AssignmentTaskItem>>(assignment.AssignedTasks) ?? new()) : new(),
                CompletedInEtut = !string.IsNullOrWhiteSpace(assignment.CompletedInEtut) ? (JsonSerializer.Deserialize<List<string>>(assignment.CompletedInEtut) ?? new()) : new(),
                CompletedAtHome = !string.IsNullOrWhiteSpace(assignment.CompletedAtHome) ? (JsonSerializer.Deserialize<List<string>>(assignment.CompletedAtHome) ?? new()) : new(),
                Status = assignment.Status,
                EtutTeacherNote = assignment.EtutTeacherNote,
                ParentNote = assignment.ParentNote
            });
        }

        var result = new PagedResult<DailyAssignmentDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<DailyAssignmentDto>>(result, "Görevler listelendi.");
    }

    private Task<Core.Utilities.Results.IResult> SendParentNotification(Student student, DailyAssignment assignment)
    {
        return _notificationService.AddAsync(new CreateParentNotificationDto
        {
            StudentId = student.Id,
            NotificationType = "homework",
            Title = "Yeni Ev Ödevi",
            Message = $"{student.FirstName} {student.LastName} için yeni ev ödevi tanımlandı. Lütfen uygulamadan kontrol edip tamamlandığında işaretleyiniz.",
            RecipientPhone = "905550000000", // İleride Parent tablosundan alınacak
            DeliveryChannel = "in_app"
        });
    }
}
