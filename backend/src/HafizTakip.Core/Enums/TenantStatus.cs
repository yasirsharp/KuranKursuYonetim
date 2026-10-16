namespace HafizTakip.Core.Enums;

/// <summary>
/// Kurs (tenant) durumları ve erişim kuralları.
/// Başvuruyla açılan kurs <see cref="Pending"/> durumunda başlar; platform yöneticisi onaylayınca
/// <see cref="Active"/>, reddederse <see cref="Rejected"/> olur.
/// </summary>
public static class TenantStatus
{
    public const string Pending = "pending";
    public const string Trial = "trial";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyCollection<string> All = new[] { Pending, Trial, Active, Suspended, Cancelled, Rejected };

    /// <summary>Giriş yapılabilen durumlar. Onay bekleyen/reddedilen kursun yöneticisi yalnızca başvuru durumunu görebilir.</summary>
    public static readonly IReadOnlyCollection<string> SignInAllowed = new[] { Pending, Trial, Active, Rejected };

    public static bool IsApplication(string status) => status is Pending or Rejected;

    public static bool CanSignIn(string status, bool isActive) => isActive && SignInAllowed.Contains(status);

    public static bool HasFullAccess(string status, bool isActive) => isActive && status is Active or Trial;
}
