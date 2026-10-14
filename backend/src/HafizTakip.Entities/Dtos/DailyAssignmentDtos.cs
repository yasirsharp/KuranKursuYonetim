using HafizTakip.Core.Entities;
using System.Text.Json;

namespace HafizTakip.Entities.Dtos;

// JSON array item'larını temsil edecek yardımcı sınıf
public class AssignmentTaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "ezber"; // ezber, has, ders
    public int Cuz { get; set; }
    public int Page { get; set; }
}

public class DailyAssignmentDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    
    public DateTime AssignmentDate { get; set; }
    public bool IsBoarderToday { get; set; }
    
    public List<AssignmentTaskItem> AssignedTasks { get; set; } = new();
    public List<string> CompletedInEtut { get; set; } = new();
    public List<string> CompletedAtHome { get; set; } = new();
    
    public string Status { get; set; } = string.Empty;
    public string? EtutTeacherNote { get; set; }
    public string? ParentNote { get; set; }
}

public class CreateDailyAssignmentDto : IDto
{
    public Guid StudentId { get; set; }
    public DateTime AssignmentDate { get; set; }
    public bool? OverrideIsBoarder { get; set; } // Hoca eğer varsayılanı ezmek isterse
    public List<AssignmentTaskItem> AssignedTasks { get; set; } = new();
}

public class CompleteEtutAssignmentDto : IDto
{
    public Guid Id { get; set; }
    public List<string> CompletedTaskIds { get; set; } = new();
    public string? EtutTeacherNote { get; set; }
    public bool FinishEtut { get; set; } = true; // Kalanlar veliye düşsün mü?
}

public class CompleteHomeAssignmentDto : IDto
{
    public Guid Id { get; set; }
    public List<string> CompletedTaskIds { get; set; } = new();
    public string? ParentNote { get; set; }
}

public class DailyAssignmentFilter : IDto
{
    public Guid? StudentId { get; set; }

    /// <summary>Doluysa yalnızca o günün görevleri.</summary>
    public DateTime? Date { get; set; }
    public bool? IsBoarderToday { get; set; }
    public string? Status { get; set; }
}