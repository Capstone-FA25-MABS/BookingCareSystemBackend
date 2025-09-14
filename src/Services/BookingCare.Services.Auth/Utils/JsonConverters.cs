using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Auth.Utils;

/// <summary>
/// Custom JSON converter to handle string to bool conversion for external APIs
/// </summary>
public class StringToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => bool.TryParse(reader.GetString(), out var result) && result,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => false
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
