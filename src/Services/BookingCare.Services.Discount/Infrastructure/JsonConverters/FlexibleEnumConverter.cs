using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookingCare.Services.Discount.Infrastructure.JsonConverters;

/// <summary>
/// JSON converter that accepts both string numbers and numeric values for enums
/// Allows "0", 0, "FIXED_AMOUNT" all to work
/// </summary>
public class FlexibleEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();

                // Try to parse as enum name first (e.g., "FIXED_AMOUNT")
                if (Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out var enumValue))
                {
                    return enumValue;
                }

                // Try to parse as numeric string (e.g., "0", "1")
                if (int.TryParse(stringValue, out var numericValue) && Enum.IsDefined(typeof(TEnum), numericValue))
                {
                    return (TEnum)(object)numericValue;
                }

                throw new JsonException($"Unable to convert \"{stringValue}\" to enum {typeof(TEnum).Name}");

            case JsonTokenType.Number:
                var intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(TEnum), intValue))
                {
                    return (TEnum)(object)intValue;
                }
                throw new JsonException($"Value {intValue} is not defined in enum {typeof(TEnum).Name}");

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when parsing enum {typeof(TEnum).Name}");
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        // Write as string name by default
        writer.WriteStringValue(value.ToString());
    }
}
