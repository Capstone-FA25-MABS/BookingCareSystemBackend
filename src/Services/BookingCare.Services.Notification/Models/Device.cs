using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BookingCare.Services.Notification.Models;

public class Device
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("name")]
    public string Name { get; set; } = "";
    
    [BsonElement("token")]
    public string Token { get; set; } = "";
    
    [BsonElement("registeredAt")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
