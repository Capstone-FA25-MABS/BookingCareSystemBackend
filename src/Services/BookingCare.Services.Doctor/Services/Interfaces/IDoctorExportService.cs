using BookingCare.Services.Doctor.Models.DTOs.Requests;

namespace BookingCare.Services.Doctor.Services.Interfaces;

public interface IDoctorExportService
{
    /// <summary>
    /// Export doctors list to Excel format
    /// </summary>
    Task<byte[]> ExportDoctorsToExcelAsync(DoctorAdvancedFilterRequest filter);

    /// <summary>
    /// Export doctors list to PDF format
    /// </summary>
    Task<byte[]> ExportDoctorsToPdfAsync(DoctorAdvancedFilterRequest filter);
}
