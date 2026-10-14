namespace HafizTakip.Business.Abstract;

/// <summary>
/// Oturumdaki kullanıcının hangi öğrencilerin verisini görebileceğini belirler.
/// Hocalar ve üstü kurs içindeki tüm öğrencileri görür; veli yalnızca kendisine
/// bağlı (student_parents) öğrencileri görür.
/// </summary>
public interface IStudentAccessScope
{
    /// <summary>Kısıt yoksa null; varsa erişilebilen öğrenci kimlikleri (boş olabilir).</summary>
    Task<IReadOnlyCollection<Guid>?> GetAccessibleStudentIdsAsync();

    Task<bool> CanAccessStudentAsync(Guid studentId);
}
