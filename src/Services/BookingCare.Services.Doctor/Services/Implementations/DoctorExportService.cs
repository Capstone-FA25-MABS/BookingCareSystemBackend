using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Services.Interfaces;
using System.Text;

namespace BookingCare.Services.Doctor.Services.Implementations;

public class DoctorExportService : IDoctorExportService
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorExportService> _logger;

    public DoctorExportService(
        IDoctorService doctorService,
        ILogger<DoctorExportService> logger
    )
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    public async Task<byte[]> ExportDoctorsToExcelAsync(DoctorAdvancedFilterRequest filter)
    {
        try
        {
            // Get all doctors matching filter (no pagination for export)
            // Use larger page size and increase timeout for export
            var filterForExport = new DoctorAdvancedFilterRequest
            {
                HospitalId = filter.HospitalId,
                SpecialtyIds = filter.SpecialtyIds,
                PositionIds = filter.PositionIds,
                ServiceTypes = filter.ServiceTypes,
                LanguageIds = filter.LanguageIds,
                SearchTerm = filter.SearchTerm,
                PageNumber = 1,
                PageSize = 1000 // Reduced from 10000 to avoid timeout
            };

            var result = await _doctorService.FilterDoctorsOptimizedAsync(filterForExport);

            // Create CSV format (simple Excel-compatible format)
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("STT,Họ và Tên,Chức vụ,Chuyên khoa,Kinh nghiệm (năm),Dịch vụ & Giá,Ngôn ngữ");

            // Data rows
            int index = 1;
            foreach (var doctor in result.Doctors)
            {
                var fullName = $"{doctor.LastName} {doctor.FirstName}";
                var position = doctor.Position?.Name ?? "N/A";
                var specialty = doctor.Specialty?.Name ?? "N/A";
                var experience = doctor.YearsOfExperience.ToString();

                // Format prices
                var prices = string.Join("; ", doctor.Prices.Select(p =>
                    $"{p.ServiceTypeName}: {p.Amount:N0} VNĐ"));

                // Format languages
                var languages = string.Join("; ", doctor.Languages.Select(l => l.Name));

                csv.AppendLine($"{index},\"{fullName}\",\"{position}\",\"{specialty}\",{experience},\"{prices}\",\"{languages}\"");
                index++;
            }

            // Convert to UTF-8 with BOM for Excel compatibility
            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());
            var result_bytes = new byte[preamble.Length + content.Length];
            Buffer.BlockCopy(preamble, 0, result_bytes, 0, preamble.Length);
            Buffer.BlockCopy(content, 0, result_bytes, preamble.Length, content.Length);

            _logger.LogInformation($"Successfully exported {result.Doctors.Count} doctors to Excel");
            return result_bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting doctors to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportDoctorsToPdfAsync(DoctorAdvancedFilterRequest filter)
    {
        try
        {
            // Get all doctors matching filter (no pagination for export)
            // Use larger page size and increase timeout for export
            var filterForExport = new DoctorAdvancedFilterRequest
            {
                HospitalId = filter.HospitalId,
                SpecialtyIds = filter.SpecialtyIds,
                PositionIds = filter.PositionIds,
                ServiceTypes = filter.ServiceTypes,
                LanguageIds = filter.LanguageIds,
                SearchTerm = filter.SearchTerm,
                PageNumber = 1,
                PageSize = 1000 // Reduced from 10000 to avoid timeout
            };

            var result = await _doctorService.FilterDoctorsOptimizedAsync(filterForExport);

            // Create print-friendly HTML
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Danh Sách Bác Sĩ</title>");
            html.AppendLine("<style>");
            html.AppendLine("@media print {");
            html.AppendLine("  @page { size: A4 landscape; margin: 1cm; }");
            html.AppendLine("  body { margin: 0; }");
            html.AppendLine("  .no-print { display: none; }");
            html.AppendLine("}");
            html.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 11px; }");
            html.AppendLine("h1 { text-align: center; color: #2c3e50; margin: 10px 0; font-size: 20px; }");
            html.AppendLine(".info { text-align: center; color: #7f8c8d; margin: 5px 0; font-size: 10px; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            html.AppendLine("th { background-color: #3498db; color: white; padding: 8px 5px; text-align: left; font-size: 11px; border: 1px solid #2980b9; }");
            html.AppendLine("td { padding: 6px 5px; border: 1px solid #ddd; font-size: 10px; }");
            html.AppendLine("tr:nth-child(even) { background-color: #f8f9fa; }");
            html.AppendLine("tr:hover { background-color: #e8f4f8; }");
            html.AppendLine(".price-item { display: block; margin: 2px 0; }");
            html.AppendLine(".lang-item { display: inline-block; margin-right: 5px; }");
            html.AppendLine(".print-btn { position: fixed; top: 10px; right: 10px; padding: 10px 20px; background: #3498db; color: white; border: none; border-radius: 5px; cursor: pointer; font-size: 14px; }");
            html.AppendLine(".print-btn:hover { background: #2980b9; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            // Print button (hidden when printing)
            html.AppendLine("<button class='print-btn no-print' onclick='window.print()'>🖨️ In PDF (Ctrl+P)</button>");

            html.AppendLine("<h1>DANH SÁCH BÁC SĨ</h1>");
            html.AppendLine($"<p class='info'>Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số: {result.TotalCount} bác sĩ</p>");

            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th style='width: 3%;'>STT</th>");
            html.AppendLine("<th style='width: 15%;'>Họ và Tên</th>");
            html.AppendLine("<th style='width: 12%;'>Chức vụ</th>");
            html.AppendLine("<th style='width: 12%;'>Chuyên khoa</th>");
            html.AppendLine("<th style='width: 8%;'>Kinh nghiệm</th>");
            html.AppendLine("<th style='width: 30%;'>Dịch vụ & Giá</th>");
            html.AppendLine("<th style='width: 20%;'>Ngôn ngữ</th>");
            html.AppendLine("</tr></thead><tbody>");

            int index = 1;
            foreach (var doctor in result.Doctors)
            {
                var fullName = $"{doctor.LastName} {doctor.FirstName}";
                var position = doctor.Position?.Name ?? "N/A";
                var specialty = doctor.Specialty?.Name ?? "N/A";
                var experience = $"{doctor.YearsOfExperience} năm";

                var prices = string.Join("", doctor.Prices.Select(p =>
                    $"<span class='price-item'>• {p.ServiceTypeName}: {p.Amount:N0} VNĐ</span>"));

                var languages = string.Join("", doctor.Languages.Select(l =>
                    $"<span class='lang-item'>• {l.Name}</span>"));

                html.AppendLine("<tr>");
                html.AppendLine($"<td style='text-align: center;'>{index}</td>");
                html.AppendLine($"<td><strong>{fullName}</strong></td>");
                html.AppendLine($"<td>{position}</td>");
                html.AppendLine($"<td>{specialty}</td>");
                html.AppendLine($"<td style='text-align: center;'>{experience}</td>");
                html.AppendLine($"<td>{prices}</td>");
                html.AppendLine($"<td>{languages}</td>");
                html.AppendLine("</tr>");
                index++;
            }

            html.AppendLine("</tbody></table>");

            // Footer
            html.AppendLine("<div class='info' style='margin-top: 20px; text-align: center;'>");
            html.AppendLine($"<p>© {DateTime.Now.Year} MedCure - Hệ thống quản lý bác sĩ</p>");
            html.AppendLine("</div>");

            html.AppendLine("</body></html>");

            _logger.LogInformation($"Successfully exported {result.Doctors.Count} doctors to PDF");
            return Encoding.UTF8.GetBytes(html.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting doctors to PDF");
            throw;
        }
    }
}
