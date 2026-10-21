namespace HafizTakip.Core.Utilities.Email;

public class EmailSettings
{
    public const string ConsoleProvider = "console";
    public const string SmtpProvider = "smtp";

    /// <summary>"console" (yalnızca geliştirme/test) veya "smtp".</summary>
    public string Provider { get; set; } = ConsoleProvider;

    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Hafız Takip";
}
