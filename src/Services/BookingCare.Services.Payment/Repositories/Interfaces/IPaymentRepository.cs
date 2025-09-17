using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface cho Payment Repository
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Lấy payment theo ID
    /// </summary>
    Task<PaymentEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lấy payment theo appointment ID
    /// </summary>
    Task<PaymentEntity?> GetByAppointmentIdAsync(Guid appointmentId);

    /// <summary>
    /// Lấy payment theo subscription ID
    /// </summary>
    Task<PaymentEntity?> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Lấy danh sách payments theo clinic ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Lấy danh sách payments theo clinic ID với phân trang
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Lấy danh sách payments theo patient ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// Lấy danh sách payments theo patient ID với phân trang
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Lấy dữ liệu thống kê payments
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);

    /// <summary>
    /// Tạo payment mới
    /// </summary>
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);

    /// <summary>
    /// Cập nhật payment
    /// </summary>
    Task<PaymentEntity> UpdateAsync(PaymentEntity payment);

    /// <summary>
    /// Xóa payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Kiểm tra payment có tồn tại không
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}