using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class EtutNoteDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    
    public DateTime NoteDate { get; set; }
    public string HomeworkStatus { get; set; } = string.Empty;
    public string? TeacherNotes { get; set; }
}

public class CreateEtutNoteDto : IDto
{
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime NoteDate { get; set; }
    public string HomeworkStatus { get; set; } = string.Empty;
    public string? TeacherNotes { get; set; }
}

public class UpdateEtutNoteDto : IDto
{
    public Guid Id { get; set; }
    public string HomeworkStatus { get; set; } = string.Empty;
    public string? TeacherNotes { get; set; }
}
