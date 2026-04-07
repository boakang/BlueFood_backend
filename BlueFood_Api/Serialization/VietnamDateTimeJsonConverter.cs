using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueFood.Api.Serialization;

public sealed class VietnamDateTimeJsonConverter : JsonConverter<DateTime>
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.DateTime;
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTimeOffset vietnamDateTime;

        if (value.Kind == DateTimeKind.Utc)
        {
            vietnamDateTime = new DateTimeOffset(value, TimeSpan.Zero).ToOffset(VietnamOffset);
        }
        else if (value.Kind == DateTimeKind.Local)
        {
            vietnamDateTime = new DateTimeOffset(value).ToOffset(VietnamOffset);
        }
        else
        {
            // datetime2 values from SQL Server are usually Kind.Unspecified.
            vietnamDateTime = new DateTimeOffset(value, VietnamOffset);
        }

        writer.WriteStringValue(vietnamDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture));
    }
}
