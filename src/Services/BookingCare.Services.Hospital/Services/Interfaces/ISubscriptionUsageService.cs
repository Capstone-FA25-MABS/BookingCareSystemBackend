using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface ISubscriptionUsageService
{
    Task<SubscriptionUsageResponse> GetUsageByHospitalIdAsync(Guid hospitalId);
    Task<bool> CheckDoctorLimitAsync(Guid hospitalId);
    Task<bool> CheckSpecialtyLimitAsync(Guid hospitalId);
    Task<bool> CheckAppointmentLimitAsync(Guid hospitalId, int additionalAppointments = 1);
    Task<bool> CheckServiceLimitAsync(Guid hospitalId);
    Task<List<SubscriptionUsageResponse>> GetUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<SubscriptionUsageAlertResponse> CheckUsageAlertsAsync(Guid hospitalId);

    // Methods to increment/decrement usage counts
    Task IncrementDoctorCountAsync(Guid hospitalId);
    Task DecrementDoctorCountAsync(Guid hospitalId);
    Task IncrementSpecialtyCountAsync(Guid hospitalId);
    Task DecrementSpecialtyCountAsync(Guid hospitalId);
    Task IncrementAppointmentCountAsync(Guid hospitalId, int count = 1);
    Task DecrementAppointmentCountAsync(Guid hospitalId, int count = 1);
    Task IncrementServiceCountAsync(Guid hospitalId);
    Task DecrementServiceCountAsync(Guid hospitalId);
}
