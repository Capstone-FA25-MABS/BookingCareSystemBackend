namespace BookingCare.Services.Content.Models.Entities;

/// <summary>
/// Common contract for entities that track created/updated timestamps.
/// Used by ContentDbContext to automatically maintain auditing fields.
/// </summary>
public interface ITimestampedEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}


