namespace HafizTakip.Core.Entities.Platform;

/// <summary>
/// Tenant (Kurum) bilgisini tutan tablo. Platform tablosudur.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "trial"; // Enums'tan eşleşir
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}
