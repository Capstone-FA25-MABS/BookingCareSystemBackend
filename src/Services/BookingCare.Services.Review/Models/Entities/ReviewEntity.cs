using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BookingCare.Services.Review.Enums;

namespace BookingCare.Services.Review.Models.Entities;

/// <summary>
/// Represents a reply to a review
/// </summary>
public class ReplyEntity
{
    /// <summary>
    /// Unique identifier for the reply
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID of the author who replied (GUID from SQL)
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Content of the reply
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the reply was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the reply was last updated
    /// </summary>
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a review entity in MongoDB
/// </summary>
[BsonCollection("reviews")]
public class ReviewEntity
{
    /// <summary>
    /// Unique identifier for the review
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID of the patient who created the review (GUID from SQL)
    /// </summary>
    [BsonElement("patientId")]
    [BsonRepresentation(BsonType.String)]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Type of target being reviewed (DOCTOR or SERVICE)
    /// </summary>
    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public TargetType TargetType { get; set; }

    /// <summary>
    /// ID of the doctor being reviewed (null if reviewing service)
    /// </summary>
    [BsonElement("doctorId")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// ID of the clinic service being reviewed (null if reviewing doctor)
    /// </summary>
    [BsonElement("clinicServiceId")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public Guid? ClinicServiceId { get; set; }

    /// <summary>
    /// Rating from 1 to 5 stars
    /// </summary>
    [BsonElement("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// Comment content
    /// </summary>
    [BsonElement("comment")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Replies to this review
    /// </summary>
    [BsonElement("replies")]
    public List<ReplyEntity> Replies { get; set; } = new();

    /// <summary>
    /// When the review was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the review was last updated
    /// </summary>
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Attribute to specify the MongoDB collection name
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}