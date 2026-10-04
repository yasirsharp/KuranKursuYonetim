using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class DashboardSummaryDto : IDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalGroups { get; set; }
    public int ActiveStudents { get; set; }
    public int HafizCount { get; set; }
    public double AverageCompletedPages { get; set; }
    public double TodayAttendanceRate { get; set; }
    public int TodayLessonCount { get; set; }
}

public class DashboardStudentProgressDto : IDto
{
    public string StudentName { get; set; } = string.Empty;
    public int CurrentPage { get; set; }
    public int CompletedPages { get; set; }
    public int TotalPages { get; set; } = 604;
    public double ProgressPercentage { get; set; }
    public bool IsHafiz { get; set; }
    public List<LessonDto> RecentLessons { get; set; } = new();
    public double AttendanceRate { get; set; }
}

public class DashboardGroupOverviewDto : IDto
{
    public string GroupName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public double AveragePage { get; set; }
    public int TodayPresent { get; set; }
    public int TodayAbsent { get; set; }
}
