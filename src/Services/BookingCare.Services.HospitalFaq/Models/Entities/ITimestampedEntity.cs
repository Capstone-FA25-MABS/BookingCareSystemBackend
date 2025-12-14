namespace BookingCare.Services.HospitalFaq.Models.Entities;

/// <summary>
/// Interface for entities that have CreatedAt and UpdatedAt timestamps
/// </summary>
public interface ITimestampedEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

