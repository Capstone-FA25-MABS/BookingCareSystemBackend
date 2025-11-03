namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// Data transfer object for building hospital subscription upgraded email
/// Used to resolve SonarQube issue: Method has too many parameters (14 parameters > 7 allowed)
/// </summary>
public class HospitalSubscriptionUpgradedEmailData
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
    /// Previous subscription plan name
    /// </summary>
    public required string PreviousPlanName { get; init; }

    /// <summary>
    /// Previous billing cycle
    /// </summary>
    public required string PreviousBillingCycle { get; init; }

    /// <summary>
    /// Previous subscription price
    /// </summary>
    public required decimal PreviousPrice { get; init; }

    /// <summary>
    /// New subscription plan name
    /// </summary>
    public required string NewPlanName { get; init; }

    /// <summary>
    /// New billing cycle
    /// </summary>
    public required string NewBillingCycle { get; init; }

    /// <summary>
    /// New subscription price
    /// </summary>
    public required decimal NewPrice { get; init; }

    /// <summary>
    /// New subscription start date
    /// </summary>
    public required DateTime NewStartDate { get; init; }

    /// <summary>
    /// New subscription end date
    /// </summary>
    public required DateTime NewEndDate { get; init; }

    /// <summary>
    /// Bonus days credited from previous subscription
    /// </summary>
    public required double BonusDays { get; init; }

    /// <summary>
    /// New maximum number of doctors allowed (null = unlimited)
    /// </summary>
    public int? NewMaxDoctors { get; init; }

    /// <summary>
    /// New maximum number of appointments per month (null = unlimited)
    /// </summary>
    public int? NewMaxAppointmentsPerMonth { get; init; }

    /// <summary>
    /// New features JSON string
    /// </summary>
    public string? NewFeatures { get; init; }
}

