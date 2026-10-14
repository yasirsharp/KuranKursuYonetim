using HafizTakip.Core.Constants;
using HafizTakip.Core.Enums;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

/// <summary>
/// Özet sayılar veritabanında COUNT/AVG ile hesaplanır; tablolar belleğe yüklenmez.
/// Devam oranında "geç gelen" öğrenci de derse katılmış sayılır.
/// </summary>
public class DashboardManager : IDashboardService
{
    private static readonly string[] TeacherRoles = { UserRole.HeadTeacher, UserRole.AssistantTeacher };
    private static readonly string[] AttendedStatuses = { AttendanceStatus.Present, AttendanceStatus.Late };

    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly IGroupDal _groupDal;
    private readonly IAttendanceDal _attendanceDal;
    private readonly ILessonDal _lessonDal;

    public DashboardManager(
        IStudentDal studentDal,
        IUserDal userDal,
        IGroupDal groupDal,
        IAttendanceDal attendanceDal,
        ILessonDal lessonDal)
    {
        _studentDal = studentDal;
        _userDal = userDal;
        _groupDal = groupDal;
        _attendanceDal = attendanceDal;
        _lessonDal = lessonDal;
    }

    public async Task<IDataResult<DashboardSummaryDto>> GetSummaryAsync()
    {
        var (today, tomorrow) = TodayRange();

        var todayAttendanceCount = await _attendanceDal.CountAsync(a => a.Date >= today && a.Date < tomorrow);
        var todayAttendedCount = await _attendanceDal.CountAsync(a =>
            a.Date >= today && a.Date < tomorrow && AttendedStatuses.Contains(a.Status));

        var summary = new DashboardSummaryDto
        {
            TotalStudents = await _studentDal.CountAsync(),
            ActiveStudents = await _studentDal.CountAsync(s => s.Status == StudentStatus.Active),
            HafizCount = await _studentDal.CountAsync(s => s.IsHafiz),
            TotalTeachers = await _userDal.CountAsync(u => TeacherRoles.Contains(u.Role)),
            TotalGroups = await _groupDal.CountAsync(),
            TodayLessonCount = await _lessonDal.CountAsync(l => l.LessonDate >= today && l.LessonDate < tomorrow),
            TodayAttendanceRate = Percentage(todayAttendedCount, todayAttendanceCount),
            AverageCompletedPages = Math.Round(await _studentDal.GetAverageCompletedPagesAsync(), 2)
        };

        return new SuccessDataResult<DashboardSummaryDto>(summary, "Dashboard özeti getirildi.");
    }

    public async Task<IDataResult<DashboardStudentProgressDto>> GetStudentProgressAsync(Guid studentId)
    {
        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        if (student == null)
            return new ErrorDataResult<DashboardStudentProgressDto>("Öğrenci bulunamadı.", ResultErrorType.NotFound);

        var totalAttendance = await _attendanceDal.CountAsync(a => a.StudentId == studentId);
        var attended = await _attendanceDal.CountAsync(a => a.StudentId == studentId && AttendedStatuses.Contains(a.Status));

        var progress = new DashboardStudentProgressDto
        {
            StudentName = $"{student.FirstName} {student.LastName}",
            CurrentPage = student.CurrentPage,
            CompletedPages = student.CompletedPages,
            TotalPages = QuranConstants.TotalPages,
            ProgressPercentage = Percentage(student.CompletedPages, QuranConstants.TotalPages),
            IsHafiz = student.IsHafiz,
            RecentLessons = await _lessonDal.GetRecentLessonsAsync(studentId, 10),
            AttendanceRate = Percentage(attended, totalAttendance)
        };

        return new SuccessDataResult<DashboardStudentProgressDto>(progress, "Öğrenci ilerlemesi getirildi.");
    }

    public async Task<IDataResult<DashboardGroupOverviewDto>> GetGroupOverviewAsync(Guid groupId)
    {
        var group = await _groupDal.GetGroupDetailAsync(groupId);
        if (group == null)
            return new ErrorDataResult<DashboardGroupOverviewDto>("Sınıf bulunamadı.", ResultErrorType.NotFound);

        var (today, tomorrow) = TodayRange();
        var studentIds = (await _studentDal.GetListAsync(s => s.GroupId == groupId)).Select(s => s.Id).ToList();

        var overview = new DashboardGroupOverviewDto
        {
            GroupName = group.Name,
            TeacherName = group.TeacherName,
            StudentCount = studentIds.Count,
            AveragePage = Math.Round(await _studentDal.GetAverageCompletedPagesAsync(groupId), 2),
            TodayPresent = await _attendanceDal.CountAsync(a =>
                studentIds.Contains(a.StudentId) && a.Date >= today && a.Date < tomorrow && AttendedStatuses.Contains(a.Status)),
            TodayAbsent = await _attendanceDal.CountAsync(a =>
                studentIds.Contains(a.StudentId) && a.Date >= today && a.Date < tomorrow && a.Status == AttendanceStatus.Absent)
        };

        return new SuccessDataResult<DashboardGroupOverviewDto>(overview, "Sınıf özeti getirildi.");
    }

    private static (DateTime Today, DateTime Tomorrow) TodayRange()
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        return (today, today.AddDays(1));
    }

    private static double Percentage(int part, int total) =>
        total > 0 ? Math.Round((double)part / total * 100, 2) : 0;
}
