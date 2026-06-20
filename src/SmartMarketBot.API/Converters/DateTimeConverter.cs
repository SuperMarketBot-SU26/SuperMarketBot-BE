using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMarketBot.API.Converters;

public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            DateTime.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value == default)
        {
            // Write as ISO 8601 string — avoids JsonSchemaExporter roundtrip deserialize failure
            // (default DateTime = DateTime.MinValue cannot be parsed back via RoundtripKind)
            writer.WriteStringValue("1970-01-01T00:00:00Z");
        }
        else
        {
            writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture));
        }
    }
}

public class DateTimeNullableConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            DateTime.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }
        if (reader.TokenType == JsonTokenType.Null) return null;
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else if (value.Value == default)
        {
            // Write as ISO 8601 string — avoids JsonSchemaExporter roundtrip deserialize failure
            writer.WriteStringValue("1970-01-01T00:00:00Z");
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
        }
    }
}
