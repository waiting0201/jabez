using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jabez.Api.Common;

/// <summary>
/// 寬鬆日期字串解析（人事資料卡 payload 反序列化用）。
/// Safari 不支援 &lt;input type="month"&gt;（退化為純文字框），前端可能送出使用者手打的
/// 年月字串；System.Text.Json 預設僅接受 ISO 8601，這裡放寬接受常見的年月 / 日期格式。
/// </summary>
public static class FlexibleDateTime
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d", "yyyy.MM.dd", "yyyy.M.d",
        "yyyy-MM", "yyyy-M", "yyyy/MM", "yyyy/M", "yyyy.MM", "yyyy.M",
    ];

    public static DateTime Parse(string value)
    {
        if (DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;
        throw new JsonException($"無法解析日期格式：{value}");
    }
}

/// <summary>DateTime?：null / 空字串 → null；其餘走 <see cref="FlexibleDateTime.Parse"/>。</summary>
public sealed class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("日期欄位必須為字串。");
        var s = reader.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : FlexibleDateTime.Parse(s.Trim());
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}

/// <summary>DateTime（非 nullable）：空值一律 JsonException（前端必填檢查先擋，此為最後防線）。</summary>
public sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("日期欄位必須為字串。");
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException("日期不可為空。");
        return FlexibleDateTime.Parse(s.Trim());
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
}
