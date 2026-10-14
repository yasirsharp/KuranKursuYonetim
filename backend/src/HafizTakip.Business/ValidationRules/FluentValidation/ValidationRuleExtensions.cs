using FluentValidation;
using HafizTakip.Core.Constants;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

/// <summary>Validator'larda tekrar eden kurallar.</summary>
public static class ValidationRuleExtensions
{
    /// <summary>Değer verilen kod listesinden biri olmalı.</summary>
    public static IRuleBuilderOptions<T, string?> MustBeOneOf<T>(
        this IRuleBuilder<T, string?> rule, IReadOnlyCollection<string> allowed, string fieldName) =>
        rule.Must(value => value != null && allowed.Contains(value))
            .WithMessage($"Geçersiz {fieldName}. Geçerli değerler: {string.Join(", ", allowed)}.");

    /// <summary>Mushaf sayfa numarası (1–604).</summary>
    public static IRuleBuilderOptions<T, int?> MustBeValidPage<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(1, QuranConstants.TotalPages)
            .WithMessage($"Sayfa numarası 1-{QuranConstants.TotalPages} arasında olmalıdır.");

    /// <summary>Cüz numarası (1–30).</summary>
    public static IRuleBuilderOptions<T, int?> MustBeValidJuz<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(1, QuranConstants.TotalJuz)
            .WithMessage($"Cüz numarası 1-{QuranConstants.TotalJuz} arasında olmalıdır.");

    /// <summary>Ders/yoklama gibi gerçekleşmiş olaylar ileri tarihli olamaz (saat dilimi için 1 gün tolerans).</summary>
    public static IRuleBuilderOptions<T, DateTime> MustNotBeInFuture<T>(this IRuleBuilder<T, DateTime> rule) =>
        rule.Must(date => date.Date <= DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("İleri tarihli kayıt girilemez.");
}
