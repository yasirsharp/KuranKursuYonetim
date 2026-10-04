using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class UserDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class CreateUserDto : IDto
{
    // TenantId middleware ile çözülecek (eğer platform tarafında oluşturulmuyorsa)
    // Şimdilik dışarıdan almak yerine backend'de çözeceğiz
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "parent";
}

public class UpdateUserDto : IDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
}
