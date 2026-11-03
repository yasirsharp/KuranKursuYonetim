namespace HafizTakip.Core.Enums;

/// <summary>Davet bağlantısı kuralları.</summary>
public static class InvitationRules
{
    /// <summary>Davetle katılınabilecek roller (veli daveti öğrenciye bağlı olacağı için ayrıdır).</summary>
    public static readonly IReadOnlyCollection<string> Roles = new[] { UserRole.HeadTeacher, UserRole.AssistantTeacher };

    public const int DefaultValidDays = 7;
    public const int MaxValidDays = 30;

    /// <summary>Karışabilecek karakterler (0/O, 1/I/L) çıkarılmış büyük harf + rakam; 8 karakter ≈ 40 bit.</summary>
    public const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int CodeLength = 8;
}
