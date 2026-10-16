using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

/// <summary>Kurs bilgileri. Reddedilen başvuru düzeltilip yeniden gönderilirken de bu kullanılır.</summary>
public class CourseApplicationInfoDto : IDto
{
    public string CourseName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
}

/// <summary>Herkese açık "Kurs oluştur" formu. Telefon, önce send-code ile gönderilen kodla doğrulanır.</summary>
public class CourseApplicationDto : CourseApplicationInfoDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Başvuran kişi kursun yöneticisi (tenant_admin) olarak kaydedilir
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
}

/// <summary>Başvuran kurs yöneticisinin gördüğü başvuru durumu.</summary>
public class CourseApplicationStatusDto : IDto
{
    public Guid TenantId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class RejectCourseApplicationDto : IDto
{
    public string Reason { get; set; } = string.Empty;
}
