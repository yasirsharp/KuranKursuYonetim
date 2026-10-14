using HafizTakip.Core.Utilities.Results;

namespace HafizTakip.Business.Abstract;

/// <summary>
/// İstekle gelen ilişkili kayıt kimliklerinin (öğrenci, grup, hoca, veli, sınav) mevcut kursa
/// ait olduğunu doğrular. Global tenant filtresi sayesinde başka kursun kaydı "bulunamadı" olur.
/// Her metot geçerliyse null, değilse yazma işlemi yerine döndürülecek hata sonucunu verir.
/// </summary>
public interface ITenantReferenceValidator
{
    Task<IResult?> EnsureStudentAsync(Guid studentId);
    Task<IResult?> EnsureStudentsAsync(IReadOnlyCollection<Guid> studentIds);
    Task<IResult?> EnsureGroupAsync(Guid groupId);
    Task<IResult?> EnsureExamAsync(Guid examId);

    /// <summary>Kullanıcı bu kursta tenant_admin, head_teacher veya assistant_teacher olmalı.</summary>
    Task<IResult?> EnsureTeacherAsync(Guid userId);

    /// <summary>Kullanıcı bu kursta veli rolünde olmalı.</summary>
    Task<IResult?> EnsureParentAsync(Guid userId);
}
