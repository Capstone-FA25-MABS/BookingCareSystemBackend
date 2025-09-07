using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Favorites.Models.Entities;

/// <summary>
/// Favorite entity for MongoDB
/// </summary>
public class FavoriteEntity
{
    /// <summary>
    /// Unique identifier as Guid
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Patient ID as Guid
    /// </summary>
    [BsonElement("patient_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID as Guid
    /// </summary>
    [BsonElement("doctor_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}