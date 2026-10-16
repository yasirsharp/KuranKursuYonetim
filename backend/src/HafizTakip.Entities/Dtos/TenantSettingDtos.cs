using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

/// <summary>Kurs ayarları (okuma ve güncelleme için aynı şekil). Saatler "HH:mm" biçimindedir.</summary>
public class TenantSettingsDto : IDto
{
    /// <summary>1 = Pazartesi … 7 = Pazar.</summary>
    public List<int> WorkingDays { get; set; } = new();
    public TimeOnly DayStartTime { get; set; }
    public TimeOnly DayEndTime { get; set; }
    public int DefaultSlotMinutes { get; set; }
    public string TimeZone { get; set; } = string.Empty;

    public int AbsenceThreshold { get; set; }
    public int AbsencePeriodDays { get; set; }

    public TimeOnly? EtutStartTime { get; set; }

    public string ReviewMode { get; set; } = string.Empty;
    public int DailyReviewTargetPages { get; set; }

    public bool NotifyParentOnAbsence { get; set; }
    public bool NotifyParentOnMilestone { get; set; }
}

/// <summary>Kurs müdürünün düzenleyebildiği kurs bilgileri.</summary>
public class CourseInfoDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class UpdateCourseInfoDto : IDto
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
