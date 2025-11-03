using BookingCare.Services.Hospital.Models.DTOs.Responses;

namespace BookingCare.Services.Hospital.Services.Interfaces;

public interface ISubscriptionUsageService
{
    Task<SubscriptionUsageResponse> GetUsageByHospitalIdAsync(Guid hospitalId);
    Task<bool> CheckDoctorLimitAsync(Guid hospitalId);
    Task<bool> CheckSpecialtyLimitAsync(Guid hospitalId);
    Task<bool> CheckAppointmentLimitAsync(Guid hospitalId, int additionalAppointments = 1);
    Task<List<SubscriptionUsageResponse>> GetUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<SubscriptionUsageAlertResponse> CheckUsageAlertsAsync(Guid hospitalId);
}
