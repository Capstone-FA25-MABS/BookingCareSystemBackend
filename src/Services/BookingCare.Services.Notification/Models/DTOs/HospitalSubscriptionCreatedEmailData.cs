namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// Data transfer object for building hospital subscription created email
/// Used to resolve SonarQube issue: Method has too many parameters (10 parameters > 7 allowed)
/// </summary>
public class HospitalSubscriptionCreatedEmailData
{
    /// <summary>
    /// Hospital name
    /// </summary>
    public required string HospitalName { get; init; }

    /// <summary>
    /// Contact person name
    /// </summary>
    public required string ContactPersonName { get; init; }

    /// <summary>
    /// Subscription plan name
    /// </summary>
    public required string PlanName { get; init; }

    /// <summary>
    /// Billing cycle (MONTHLY, QUARTERLY, YEARLY)
    /// </summary>
    public required string BillingCycle { get; init; }

    /// <summary>
    /// Subscription price
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Subscription start date
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Subscription end date
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Maximum number of doctors allowed (null = unlimited)
    /// </summary>
    public int? MaxDoctors { get; init; }

    /// <summary>
    /// Maximum number of appointments per month (null = unlimited)
    /// </summary>
    public int? MaxAppointmentsPerMonth { get; init; }

    /// <summary>
    /// Features JSON string
    /// </summary>
    public string? Features { get; init; }
}

