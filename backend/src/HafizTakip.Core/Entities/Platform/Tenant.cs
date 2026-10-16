namespace HafizTakip.Core.Entities.Platform;

/// <summary>
/// Tenant (Kurum) bilgisini tutan tablo. Platform tablosudur.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "trial"; // Değerler: Core.Enums.TenantStatus
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    // Kurs başvurusu ve platform onayı
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }

    /// <summary>Başvurunun (son) gönderilme zamanı; platform yöneticisinin açtığı kurslarda null.</summary>
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Onaylayan/reddeden platform kullanıcısı.</summary>
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
}
