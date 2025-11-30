namespace BookingCare.Services.Payment.Models.DTOs.Responses;

/// <summary>
/// DTO for hospital with pending payouts information
/// </summary>
public class PendingHospitalInfoResponse
{
    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// Total amount to be paid
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of completed appointments
    /// </summary>
    public int AppointmentCount { get; set; }

    /// <summary>
    /// Whether hospital has registered bank account
    /// </summary>
    public bool HasBankAccount { get; set; }
}
