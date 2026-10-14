using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

// Exam DTOs
public class ExamDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    public string ExamType { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateExamDto : IDto
{
    public Guid GroupId { get; set; }
    public string ExamType { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateExamDto : IDto
{
    public Guid Id { get; set; }
    public string ExamType { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ExamResult DTOs
public class ExamResultDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
    public string? TeacherNotes { get; set; }
}

public class CreateExamResultDto : IDto
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
    public string? TeacherNotes { get; set; }
}

public class UpdateExamResultDto : IDto
{
    public Guid Id { get; set; }
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
    public string? TeacherNotes { get; set; }
}

public class ExamFilter : IDto
{
    public Guid? GroupId { get; set; }
}

public class ExamResultFilter : IDto
{
    public Guid? ExamId { get; set; }
    public Guid? StudentId { get; set; }
}