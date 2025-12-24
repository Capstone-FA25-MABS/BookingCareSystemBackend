namespace BookingCare.Services.Content.Models.Entities;

/// <summary>
/// Entity for the hospital_faqs table - stores FAQs (Frequently Asked Questions) for hospitals.
/// This is migrated from BookingCare.Services.HospitalFaq to the unified Content service.
/// </summary>
public class HospitalFaqEntity : ITimestampedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the hospital this FAQ belongs to.
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// The question text.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The answer text.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Account ID of the staff member who created this FAQ.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Display order for sorting FAQs (lower numbers appear first).
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime UpdatedAt { get; set; }
}


