using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Kullanıcı tablosu (Öğretmen, Veli, Yönetici vb.)
/// </summary>
public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty; // Login için primary
    public string? Email { get; set; }
    public string Role { get; set; } = "parent"; // super_admin, tenant_admin, head_teacher...
    
    public string? PasswordHash { get; set; } // OTP dışı login gerekirse diye opsiyonel
}
