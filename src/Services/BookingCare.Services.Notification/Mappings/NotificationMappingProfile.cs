using AutoMapper;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Models.Entities;
using MongoDB.Bson;
using System.Text.Json;

namespace BookingCare.Services.Notification.Mappings;

/// <summary>
/// AutoMapper profile for Notification Service
/// </summary>
public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // CreateNotificationDto -> NotificationEntity
        CreateMap<CreateNotificationDto, NotificationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src =>
                src.ExpirationDays > 0
                    ? DateTime.UtcNow.AddDays(src.ExpirationDays)
                    : (DateTime?)null))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => ConvertDictToBson(src.Metadata)));

        // NotificationEntity -> NotificationDto
        CreateMap<NotificationEntity, NotificationDto>()
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => ConvertBsonToDict(src.Metadata)));
    }

    /// <summary>
    /// Convert Dictionary to BsonDocument, handling JsonElement
    /// </summary>
    private static BsonDocument? ConvertDictToBson(Dictionary<string, object>? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var bsonDoc = new BsonDocument();
        foreach (var kvp in dict)
        {
            bsonDoc.Add(kvp.Key, ConvertObjectToBsonValue(kvp.Value));
        }
        return bsonDoc;
    }

    /// <summary>
    /// Convert object to BsonValue, handling JsonElement
    /// </summary>
    private static BsonValue ConvertObjectToBsonValue(object? value)
    {
        if (value == null) return BsonNull.Value;

        // Handle JsonElement (from event bus deserialization)
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.TryGetInt32(out var intValue) ? intValue : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => BsonNull.Value,
                JsonValueKind.Object => ConvertJsonObjectToBson(jsonElement),
                JsonValueKind.Array => new BsonArray(jsonElement.EnumerateArray().Select(e => ConvertObjectToBsonValue(e))),
                _ => jsonElement.ToString()
            };
        }

        // Handle standard types
        return value switch
        {
            string s => s,
            int i => i,
            long l => l,
            double d => d,
            bool b => b,
            DateTime dt => dt,
            Dictionary<string, object> dict => ConvertDictToBson(dict)!,
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Convert JsonElement object to BsonDocument
    /// </summary>
    private static BsonDocument ConvertJsonObjectToBson(JsonElement jsonElement)
    {
        var bsonDoc = new BsonDocument();
        foreach (var property in jsonElement.EnumerateObject())
        {
            bsonDoc.Add(property.Name, ConvertObjectToBsonValue(property.Value));
        }
        return bsonDoc;
    }

    /// <summary>
    /// Convert BsonDocument to Dictionary
    /// </summary>
    private static Dictionary<string, object>? ConvertBsonToDict(BsonDocument? bsonDoc)
    {
        if (bsonDoc == null) return null;

        return bsonDoc.ToDictionary(
            element => element.Name,
            element => ConvertBsonValueToObject(element.Value)
        );
    }

    /// <summary>
    /// Convert BsonValue to object
    /// </summary>
    private static object ConvertBsonValueToObject(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.String => value.AsString,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Null => null!,
            BsonType.Document => ConvertBsonToDict(value.AsBsonDocument)!,
            BsonType.Array => value.AsBsonArray.Select(ConvertBsonValueToObject).ToList(),
            _ => value.ToString()!
        };
    }
}

