using System;

namespace HafizTakip.Entities.Dtos;

// --- LESSON DTOs ---

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    
    public int? CuzNo { get; set; }
    public int? PageNo { get; set; }
    public string SurahName { get; set; } = string.Empty;
    
    public bool IsNewPage { get; set; }
    public string Performance { get; set; } = "iyi";
    public string HomeworkStatus { get; set; } = "yapilmadi";
    public string? TeacherNotes { get; set; }
}

public class CreateLessonDto
{
    public Guid StudentId { get; set; }
    /// <summary>Boşsa dersi kaydeden kullanıcı hoca olarak yazılır.</summary>
    public Guid? TeacherId { get; set; }
    public DateTime LessonDate { get; set; }
    
    public int? CuzNo { get; set; }
    public int? PageNo { get; set; }
    public string? SurahName { get; set; }
    
    public bool IsNewPage { get; set; }
    public string Performance { get; set; } = "iyi";
    public string HomeworkStatus { get; set; } = "yapilmadi";
    public string? TeacherNotes { get; set; }
}

public class UpdateLessonDto
{
    public Guid Id { get; set; }
    public int? CuzNo { get; set; }
    public int? PageNo { get; set; }
    public string? SurahName { get; set; }
    public string Performance { get; set; } = "iyi";
    public string HomeworkStatus { get; set; } = "yapilmadi";
    public string? TeacherNotes { get; set; }
}

/// <summary>Ders listesi filtreleri (query string'den bağlanır).</summary>
public class LessonFilter
{
    public Guid? StudentId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}