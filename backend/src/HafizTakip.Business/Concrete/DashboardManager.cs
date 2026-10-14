using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class DashboardManager : IDashboardService
{
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
        var allStudents = await _studentDal.GetListAsync();
        var totalStudents = allStudents.Count;
        var activeStudents = allStudents.Count(s => s.Status == "active");
        var hafizCount = allStudents.Count(s => s.IsHafiz);
        var averageCompletedPages = totalStudents > 0 ? allStudents.Average(s => (double)s.CompletedPages) : 0.0;

        var allUsers = await _userDal.GetListAsync();
        var totalTeachers = allUsers.Count(u => u.Role == "head_teacher" || u.Role == "assistant_teacher");

        var allGroups = await _groupDal.GetListAsync();
        var totalGroups = allGroups.Count;

        var today = DateTime.UtcNow.Date;

        var allLessons = await _lessonDal.GetListAsync();
        var todayLessons = allLessons.Count(l => l.LessonDate == today);

        var allAttendances = await _attendanceDal.GetListAsync();
        var todayAttendances = allAttendances.Where(a => a.Date == today).ToList();
        var presentCount = todayAttendances.Count(a => a.Status == "var");
        double attendanceRate = todayAttendances.Count > 0
            ? (double)presentCount / todayAttendances.Count * 100
            : 0;

        var summary = new DashboardSummaryDto
        {
            TotalStudents = totalStudents,
            ActiveStudents = activeStudents,
            HafizCount = hafizCount,
            TotalTeachers = totalTeachers,
            TotalGroups = totalGroups,
            TodayLessonCount = todayLessons,
            TodayAttendanceRate = Math.Round(attendanceRate, 2),
            AverageCompletedPages = Math.Round(averageCompletedPages, 2)
        };

        return new SuccessDataResult<DashboardSummaryDto>(summary, "Dashboard özeti getirildi.");
    }

    public async Task<IDataResult<DashboardStudentProgressDto>> GetStudentProgressAsync(Guid studentId)
    {
        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        if (student == null) return new ErrorDataResult<DashboardStudentProgressDto>("Öğrenci bulunamadı.");

        var allLessons = await _lessonDal.GetListAsync(l => l.StudentId == studentId);
        var recentLessonsDto = allLessons
            .OrderByDescending(l => l.LessonDate)
            .Take(10)
            .Select(l => new LessonDto
            {
                Id = l.Id,
                LessonDate = l.LessonDate,
                CuzNo = l.JuzNumber,
                PageNo = l.PageNumber,
                SurahName = l.SuraId.ToString(),
                IsNewPage = l.IsNewPage,
                Performance = l.Performance,
                TeacherNotes = l.Notes
            }).ToList();

        var allAttendances = await _attendanceDal.GetListAsync(a => a.StudentId == studentId);
        double attendanceRate = 0;
        if (allAttendances.Any())
        {
            var present = allAttendances.Count(a => a.Status == "var");
            attendanceRate = (double)present / allAttendances.Count * 100;
        }

        var progress = new DashboardStudentProgressDto
        {
            StudentName = $"{student.FirstName} {student.LastName}",
            CurrentPage = student.CurrentPage,
            CompletedPages = student.CompletedPages,
            TotalPages = 604,
            ProgressPercentage = Math.Round((double)student.CompletedPages / 604 * 100, 2),
            IsHafiz = student.IsHafiz,
            RecentLessons = recentLessonsDto,
            AttendanceRate = Math.Round(attendanceRate, 2)
        };

        return new SuccessDataResult<DashboardStudentProgressDto>(progress, "Öğrenci ilerlemesi getirildi.");
    }

    public async Task<IDataResult<DashboardGroupOverviewDto>> GetGroupOverviewAsync(Guid groupId)
    {
        var group = await _groupDal.GetAsync(g => g.Id == groupId);
        if (group == null) return new ErrorDataResult<DashboardGroupOverviewDto>("Sınıf bulunamadı.");

        var teacher = await _userDal.GetAsync(u => u.Id == group.TeacherId);
        
        var students = await _studentDal.GetListAsync(s => s.GroupId == groupId);
        var studentCount = students.Count;
        var averagePage = studentCount > 0 ? students.Average(s => (double)s.CompletedPages) : 0.0;

        var today = DateTime.UtcNow.Date;
        var studentIds = students.Select(s => s.Id).ToHashSet();
        var allAttendances = await _attendanceDal.GetListAsync(a => a.Date == today);
        var todayAttendances = allAttendances.Where(a => studentIds.Contains(a.StudentId)).ToList();

        var todayPresent = todayAttendances.Count(a => a.Status == "var");
        var todayAbsent = todayAttendances.Count(a => a.Status == "yok");

        var overview = new DashboardGroupOverviewDto
        {
            GroupName = group.Name,
            TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
            StudentCount = studentCount,
            AveragePage = Math.Round(averagePage, 2),
            TodayPresent = todayPresent,
            TodayAbsent = todayAbsent
        };

        return new SuccessDataResult<DashboardGroupOverviewDto>(overview, "Sınıf özeti getirildi.");
    }
}
