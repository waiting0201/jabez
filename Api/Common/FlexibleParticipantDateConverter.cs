using System.Text.Json;
using System.Text.Json.Serialization;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Common;

/// <summary>
/// 假日執行活動參與日期反序列化：接受兩種形狀 —
///   "2026-08-02"                          （Slot 上線前的舊版前端，視為 full）
///   { "date": "2026-08-02", "slot": "am" }（新版；slot 缺席視為 full）
/// 用途是部署空窗期防護：舊版 SPA 快取殘留仍送純字串陣列時，不會炸成 500。
/// 比照 <see cref="FlexibleDateTimeConverter"/> 的寬鬆解析模式。
/// </summary>
public sealed class FlexibleParticipantDateConverter : JsonConverter<ParticipantDateRequest>
{
    public override ParticipantDateRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException("參與日期不可為空。");
            return new ParticipantDateRequest(FlexibleDateTime.Parse(s.Trim()), ParticipantDateSlots.Full);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("參與日期格式無效。");

        DateTime? date = null;
        string?   slot = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var prop = reader.GetString();
            reader.Read();

            if (string.Equals(prop, "date", StringComparison.OrdinalIgnoreCase))
            {
                var s = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                date = string.IsNullOrWhiteSpace(s) ? null : FlexibleDateTime.Parse(s.Trim());
            }
            else if (string.Equals(prop, "slot", StringComparison.OrdinalIgnoreCase))
            {
                slot = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            }
            else
            {
                reader.Skip();
            }
        }

        if (date is null) throw new JsonException("參與日期缺少 date 欄位。");
        return new ParticipantDateRequest(date.Value, slot);
    }

    public override void Write(Utf8JsonWriter writer, ParticipantDateRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("date", value.Date.ToString("yyyy-MM-dd"));
        writer.WriteString("slot", ParticipantDateSlots.Normalize(value.Slot));
        writer.WriteEndObject();
    }
}
