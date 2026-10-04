using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;
using HafizTakip.DataAccess.Concrete.EntityFramework;

namespace HafizTakip.Business.Concrete;

public class DashboardManager : IDashboardService
{
    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly IGroupDal _groupDal;
    private readonly IAttendanceDal _attendanceDal;
    private readonly ILessonDal _lessonDal;
    private readonly HafizTakipDbContext _context;

    public DashboardManager(
        IStudentDal studentDal, 
        IUserDal userDal, 
        IGroupDal groupDal, 
        IAttendanceDal attendanceDal, 
        ILessonDal lessonDal,
        HafizTakipDbContext context)
    {
        _studentDal = studentDal;
        _userDal = userDal;
        _groupDal = groupDal;
        _attendanceDal = attendanceDal;
        _lessonDal = lessonDal;
        _context = context;
    }

    public async Task<IDataResult<DashboardSummaryDto>> GetSummaryAsync()
    {
        var totalStudents = await _context.Students.CountAsync();
        // TODO: Öğrenci durumlarını ortak enum/sabitlerle yönet; oluşturma akışı "active" yazarken bu sayaç "Aktif" arıyor.
        var activeStudents = await _context.Students.CountAsync(s => s.Status == "Aktif");
        var hafizCount = await _context.Students.CountAsync(s => s.IsHafiz);
        var totalTeachers = await _context.Users.CountAsync(u => u.Role == "head_teacher" || u.Role == "assistant_teacher");
        var totalGroups = await _context.Groups.CountAsync();

        var today = DateTime.UtcNow.Date;
        var todayLessons = await _context.Lessons.CountAsync(l => l.LessonDate == today);
        
        var todayAttendances = await _context.Attendances.Where(a => a.Date == today).ToListAsync();
        var presentCount = todayAttendances.Count(a => a.Status == "var");
        
        double attendanceRate = 0;
        if (todayAttendances.Any())
        {
            attendanceRate = (double)presentCount / todayAttendances.Count * 100;
        }

        var averageCompletedPages = 0.0;
        if (totalStudents > 0)
        {
            averageCompletedPages = await _context.Students.AverageAsync(s => s.CompletedPages);
        }

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

        var recentLessonsEntity = await _context.Lessons
            .Where(l => l.StudentId == studentId)
            .OrderByDescending(l => l.LessonDate)
            .Take(10)
            .ToListAsync();

        var recentLessonsDto = recentLessonsEntity.Select(l => new LessonDto
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

        var attendances = await _context.Attendances.Where(a => a.StudentId == studentId).ToListAsync();
        double attendanceRate = 0;
        if (attendances.Any())
        {
            var present = attendances.Count(a => a.Status == "var");
            attendanceRate = (double)present / attendances.Count * 100;
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
        
        var students = await _context.Students.Where(s => s.GroupId == groupId).ToListAsync();
        var studentCount = students.Count;
        
        var averagePage = 0.0;
        if (studentCount > 0)
        {
            averagePage = students.Average(s => s.CompletedPages);
        }

        var today = DateTime.UtcNow.Date;
        
        var studentIds = students.Select(s => s.Id).ToList();
        var todayAttendances = await _context.Attendances
            .Where(a => a.Date == today && studentIds.Contains(a.StudentId))
            .ToListAsync();

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
