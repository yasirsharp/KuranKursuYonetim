using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class StudentDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    
    public int CurrentPage { get; set; }
    public int CompletedPages { get; set; }
    public bool IsHafiz { get; set; }
    public bool IsBoarder { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateStudentDto : IDto
{
    public Guid? GroupId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public bool IsBoarder { get; set; } = false;
}

public class UpdateStudentDto : IDto
{
    public Guid Id { get; set; }
    public Guid? GroupId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsBoarder { get; set; }
}

/// <summary>Öğrenci listesi filtreleri (query string'den bağlanır).</summary>
public class StudentFilter : IDto
{
    /// <summary>Ad, soyad veya kimlik numarasında arar.</summary>
    public string? Search { get; set; }
    public Guid? GroupId { get; set; }
    public string? Status { get; set; }
}