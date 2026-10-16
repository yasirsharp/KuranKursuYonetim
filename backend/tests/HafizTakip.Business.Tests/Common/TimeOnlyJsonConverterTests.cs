using System.Text.Json;
using HafizTakip.Core.Utilities.Json;

namespace HafizTakip.Business.Tests.Common;

public class TimeOnlyJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new TimeOnlyJsonConverter() } };

    private sealed record Holder(TimeOnly Start, TimeOnly? Optional);

    [Theory]
    [InlineData("\"08:30\"", 8, 30)]
    [InlineData("\"08:30:00\"", 8, 30)]
    [InlineData("\"23:05\"", 23, 5)]
    public void Read_ShouldAcceptShortAndLongForms(string json, int hour, int minute)
    {
        JsonSerializer.Deserialize<TimeOnly>(json, Options).Should().Be(new TimeOnly(hour, minute));
    }

    [Theory]
    [InlineData("\"25:00\"")]
    [InlineData("\"8.30\"")]
    public void Read_InvalidTime_ShouldThrow(string json)
    {
        var act = () => JsonSerializer.Deserialize<TimeOnly>(json, Options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Write_ShouldUseHourMinuteAndHandleNullable()
    {
        var json = JsonSerializer.Serialize(new Holder(new TimeOnly(9, 5), null), Options);
        json.Should().Be("{\"Start\":\"09:05\",\"Optional\":null}");

        JsonSerializer.Deserialize<Holder>("{\"Start\":\"10:00\",\"Optional\":\"19:30\"}", Options)!
            .Optional.Should().Be(new TimeOnly(19, 30));
    }
}