using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Kurs ayarları (kurs başına tek satır).</summary>
public class TenantSetting : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Çalışma günleri: 1 = Pazartesi … 7 = Pazar (ISO).</summary>
    public int[] WorkingDays { get; set; } = { 1, 2, 3, 4, 5, 6 };
    public TimeOnly DayStartTime { get; set; } = new(8, 0);
    public TimeOnly DayEndTime { get; set; } = new(17, 0);
    public int DefaultSlotMinutes { get; set; } = 45;
    public string TimeZone { get; set; } = "Europe/Istanbul";

    public int AbsenceThreshold { get; set; } = 5;
    public int AbsencePeriodDays { get; set; } = 30;

    public TimeOnly? EtutStartTime { get; set; }

    public string ReviewMode { get; set; } = "spaced";
    public int DailyReviewTargetPages { get; set; } = 20;

    public bool NotifyParentOnAbsence { get; set; } = true;
    public bool NotifyParentOnMilestone { get; set; } = true;
}
