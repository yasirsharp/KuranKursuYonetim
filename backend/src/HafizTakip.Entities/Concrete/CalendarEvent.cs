using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Kurs takvimi kaydı: tatil, ara tatil, kapalı gün veya etkinlik (bir ya da birkaç gün).</summary>
public class CalendarEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Kaydın düştüğü dönem (başlangıç tarihine göre kendiliğinden atanır).</summary>
    public Guid? TermId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = "event";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>true ise bu günler ders günü sayılmaz.</summary>
    public bool IsDayOff { get; set; } = true;

    public string? Description { get; set; }

    /// <summary>Hazır tatil listesinden eklenen kayıtların anahtarı (ör. "2027-ramazan-bayrami"); elle eklenenlerde boş.</summary>
    public string? HolidayKey { get; set; }
}
