using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Kursa personel daveti: herkese açık bağlantı/QR (…/katil/{Code}).</summary>
public class Invitation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Bağlantıdaki kod; tüm kurslarda benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Katılan kişinin alacağı rol (head_teacher | assistant_teacher).</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>Daveti oluşturan kullanıcı.</summary>
    public Guid? CreatedBy { get; set; }
}
