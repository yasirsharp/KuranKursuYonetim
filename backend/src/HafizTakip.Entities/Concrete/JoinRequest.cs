using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Davetle gelen kişinin katılım isteği; yönetici onaylayınca personel olarak eklenir.</summary>
public class JoinRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid InvitationId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>SMS koduyla doğrulanmış, normalize numara (5XXXXXXXXX).</summary>
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Onaylanınca oluşturulan kullanıcı.</summary>
    public Guid? UserId { get; set; }
}
