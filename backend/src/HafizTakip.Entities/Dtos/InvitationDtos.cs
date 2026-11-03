using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

/// <summary>Yönetici ekranı: geçerli davet. Bağlantı istemcide {web}/katil/{Code} olarak kurulur.</summary>
public class InvitationDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInvitationDto : IDto
{
    public string Role { get; set; } = string.Empty;

    /// <summary>Kaç gün geçerli (1–30); boşsa 7.</summary>
    public int? ValidDays { get; set; }
}

/// <summary>Herkese açık davet sayfası: hangi kursa, hangi rolle davet edildiği.</summary>
public class InvitationPublicDto : IDto
{
    public string Code { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? District { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class JoinWithInvitationDto : IDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Telefona SMS ile gelen doğrulama kodu.</summary>
    public string Code { get; set; } = string.Empty;
}

public class JoinRequestDto : IDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class RejectJoinRequestDto : IDto
{
    public string? Reason { get; set; }
}
