using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HafizTakip.Core.Utilities.Json;

/// <summary>
/// Saatleri "HH:mm" olarak yazar; okurken "HH:mm" ve "HH:mm:ss" biçimlerini kabul eder.
/// (Varsayılan System.Text.Json yalnızca "HH:mm:ss" kabul eder; arayüzlerde saat genellikle "HH:mm" girilir.)
/// </summary>
public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats = { "HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (TimeOnly.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time;

        throw new JsonException($"Geçersiz saat: '{value}'. Beklenen biçim: SS:dd (ör. 08:30).");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
}
