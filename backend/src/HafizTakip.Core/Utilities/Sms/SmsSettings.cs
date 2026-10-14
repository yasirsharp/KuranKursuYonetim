namespace HafizTakip.Core.Utilities.Sms;

public class SmsSettings
{
    public const string ConsoleProvider = "console";
    public const string NetGsmProvider = "netgsm";

    /// <summary>"console" (yalnızca geliştirme) veya "netgsm".</summary>
    public string Provider { get; set; } = ConsoleProvider;

    public NetGsmSettings NetGsm { get; set; } = new();
}

public class NetGsmSettings
{
    public string BaseUrl { get; set; } = "https://api.netgsm.com.tr";
    public string UserCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>NetGSM panelinde onaylı mesaj başlığı.</summary>
    public string Header { get; set; } = string.Empty;
}