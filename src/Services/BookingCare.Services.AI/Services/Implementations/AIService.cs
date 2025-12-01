using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service implementation for AI operations using Google Gemini API
/// </summary>
public class AIService : IAIService
{
    private readonly GeminiApiHelper _geminiApiHelper;
    private readonly ServiceGeminiConfiguration _serviceConfig;
    private readonly ILogger<AIService> _logger;

    public AIService(
        GeminiApiHelper geminiApiHelper,
        IOptions<GeminiServicesConfiguration> geminiServicesConfig,
        ILogger<AIService> logger
    )
    {
        _geminiApiHelper = geminiApiHelper;
        _serviceConfig = geminiServicesConfig.Value.MedicalSummary;
        _logger = logger;
    }

    public async Task<MedicalSummaryResponse> GenerateMedicalSummaryAsync(
        GenerateMedicalSummaryRequest request
    )
    {
        try
        {
            _logger.LogInformation(
                "Generating medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            // Build the prompt for Gemini
            var prompt = BuildMedicalSummaryPrompt(request);

            // Call Gemini API
            var geminiResponse = await CallGeminiApiAsync(prompt);

            _logger.LogInformation(
                "Successfully generated medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            return new MedicalSummaryResponse
            {
                Summary = geminiResponse,
                AppointmentId = request.AppointmentId,
                GeneratedAt = DateTime.UtcNow,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating medical summary for appointment {AppointmentId}",
                request.AppointmentId
            );

            return new MedicalSummaryResponse
            {
                Summary = string.Empty,
                AppointmentId = request.AppointmentId,
                GeneratedAt = DateTime.UtcNow,
                Success = false,
                ErrorMessage = $"Lỗi khi tạo tóm tắt: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Build medical summary prompt for Gemini AI
    /// </summary>
    private static string BuildMedicalSummaryPrompt(GenerateMedicalSummaryRequest request)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine(
            "Bạn là một trợ lý y tế AI chuyên nghiệp. Nhiệm vụ của bạn là tóm tắt cuộc trò chuyện khám bệnh giữa bác sĩ và bệnh nhân thành một hồ sơ y tế có cấu trúc, đẹp mắt và chuyên nghiệp."
        );
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**THÔNG TIN CUỘC HẸN:**");

        if (!string.IsNullOrEmpty(request.DoctorName))
            promptBuilder.AppendLine($"- Bác sĩ: {request.DoctorName}");

        if (!string.IsNullOrEmpty(request.PatientName))
            promptBuilder.AppendLine($"- Bệnh nhân: {request.PatientName}");

        if (request.AppointmentDate.HasValue)
        {
            promptBuilder.AppendLine(
                $"- Ngày khám: {request.AppointmentDate.Value:dd/MM/yyyy HH:mm}"
            );
        }
        else
        {
            // Use Vietnam current time when appointment date is not provided
            promptBuilder.AppendLine($"- Ngày khám: {GetVietnamNow():dd/MM/yyyy HH:mm}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**CUỘC TRÒ CHUYỆN:**");
        promptBuilder.AppendLine(request.Transcript);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**YÊU CẦU TÓM TẮT:**");
        promptBuilder.AppendLine(
            "Hãy tóm tắt cuộc trò chuyện trên theo định dạng hồ sơ bệnh án chuyên nghiệp với template đẹp mắt như sau:"
        );
        promptBuilder.AppendLine();

        // Template header
        AppendDoubleSeparator(promptBuilder);
        promptBuilder.AppendLine("                    📋 HỒ SƠ KHÁM BỆNH");
        AppendDoubleSeparator(promptBuilder);
        promptBuilder.AppendLine();

        // Medical sections
        AppendMedicalSection(
            promptBuilder,
            "🩺 TRIỆU CHỨNG",
            "Mô tả các triệu chứng chính mà bệnh nhân trình bày:",
            new[]
            {
                "• [Liệt kê từng triệu chứng với bullet points]",
                "• Bao gồm thời gian xuất hiện, mức độ nghiêm trọng nếu có"
            }
        );

        AppendMedicalSection(
            promptBuilder,
            "📜 TIỀN SỬ BỆNH",
            "Thông tin về tiền sử bệnh lý có liên quan:",
            new[]
            {
                "• [Liệt kê các bệnh lý đã có]",
                "• Thuốc đang sử dụng (nếu có)",
                "• Dị ứng thuốc (nếu có)"
            }
        );

        AppendMedicalSection(
            promptBuilder,
            "🔍 KHÁM LÂM SÀNG",
            "Kết quả khám lâm sàng:",
            new[]
            {
                "• Các chỉ số sinh tồn (huyết áp, mạch, nhiệt độ nếu có)",
                "• Kết quả khám chi tiết theo từng cơ quan/hệ thống"
            }
        );

        // Diagnosis section (slightly different format)
        AppendSectionHeader(promptBuilder, "💊 CHẨN ĐOÁN SƠ BỘ");
        promptBuilder.AppendLine("**Chẩn đoán:** [Ghi rõ chẩn đoán của bác sĩ]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**Đánh giá:** [Mức độ nghiêm trọng, tiên lượng]");
        AppendSectionSeparator(promptBuilder);

        // Treatment section with subsections
        AppendSectionHeader(promptBuilder, "💉 HƯỚNG XỬ TRÍ");
        promptBuilder.AppendLine("### Đơn thuốc:");
        promptBuilder.AppendLine("1. [Tên thuốc] - [Liều lượng] - [Cách dùng]");
        promptBuilder.AppendLine("2. [Tên thuốc] - [Liều lượng] - [Cách dùng]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Hướng dẫn điều trị:");
        promptBuilder.AppendLine("• [Các hướng dẫn chăm sóc tại nhà]");
        promptBuilder.AppendLine("• [Chế độ ăn uống, sinh hoạt]");
        AppendSectionSeparator(promptBuilder);

        AppendMedicalSection(
            promptBuilder,
            "⚠️ LƯU Ý ĐẶC BIỆT",
            null,
            new[]
            {
                "• [Các triệu chứng cần theo dõi]",
                "• [Khi nào cần tái khám]",
                "• [Các lưu ý quan trọng khác]"
            },
            addSeparatorAfter: false
        );

        // Template footer
        AppendDoubleSeparator(promptBuilder);
        promptBuilder.AppendLine();

        // Formatting instructions
        AppendInstructions(promptBuilder);

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Append a medical section with header, description and bullet points
    /// </summary>
    private static void AppendMedicalSection(
        StringBuilder builder,
        string title,
        string? description,
        string[] bulletPoints,
        bool addSeparatorAfter = true
    )
    {
        AppendSectionHeader(builder, title);

        if (!string.IsNullOrEmpty(description))
        {
            builder.AppendLine(description);
        }

        foreach (var point in bulletPoints)
        {
            builder.AppendLine(point);
        }

        if (addSeparatorAfter)
        {
            AppendSectionSeparator(builder);
        }
        else
        {
            builder.AppendLine();
        }
    }

    /// <summary>
    /// Append section header with title
    /// </summary>
    private static void AppendSectionHeader(StringBuilder builder, string title)
    {
        builder.AppendLine($"## {title}");
    }

    /// <summary>
    /// Append single-line separator
    /// </summary>
    private static void AppendSectionSeparator(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("───────────────────────────────────────────────────────────");
        builder.AppendLine();
    }

    /// <summary>
    /// Append double-line separator (for header/footer)
    /// </summary>
    private static void AppendDoubleSeparator(StringBuilder builder)
    {
        builder.AppendLine("═══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Append formatting and content instructions
    /// </summary>
    private static void AppendInstructions(StringBuilder builder)
    {
        builder.AppendLine("**HƯỚNG DẪN ĐỊNH DẠNG:**");

        var formatInstructions = new[]
        {
            "- Sử dụng CHÍNH XÁC template trên với các ký hiệu đường kẻ (═, ─)",
            "- Giữ nguyên các emoji (🩺, 📜, 🔍, 💊, 💉, ⚠️, 📋) để tạo điểm nhấn",
            "- Sử dụng bullet points (•) cho các danh sách",
            "- Sử dụng số thứ tự (1., 2., 3.) cho đơn thuốc",
            "- Giữ khoảng cách và căn lề đẹp mắt",
            "- Sử dụng **bold** cho tiêu đề quan trọng"
        };

        foreach (var instruction in formatInstructions)
        {
            builder.AppendLine(instruction);
        }

        builder.AppendLine();
        builder.AppendLine("**QUY TẮC NỘI DUNG:**");

        var contentRules = new[]
        {
            "- Chỉ tóm tắt thông tin có trong cuộc trò chuyện, KHÔNG tự thêm thông tin",
            "- Sử dụng ngôn ngữ y tế chuyên nghiệp nhưng dễ hiểu",
            "- Nếu thiếu thông tin cho mục nào, ghi '*Không có thông tin*' với font chữ nghiêng",
            "- Giữ tóm tắt ngắn gọn, súc tích nhưng đầy đủ thông tin",
            "- Sử dụng tiếng Việt có dấu chính xác",
            "- Thể hiện sự chuyên nghiệp và tỉ mỉ trong từng chi tiết"
        };

        foreach (var rule in contentRules)
        {
            builder.AppendLine(rule);
        }
    }

    /// <summary>
    /// Call Gemini API to generate content with fallback support
    /// </summary>
    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        return await _geminiApiHelper.CallGeminiApiAsync(
            prompt,
            _serviceConfig,
            temperature: null, // Use default from common config
            maxOutputTokens: null, // Use default from common config
            cancellationToken: default);
    }



    // Helper: return current time in Vietnam timezone (handles Windows and Linux time zone ids)
    private static DateTime GetVietnamNow()
    {
        try
        {
            var tz = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") // Windows
                : TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux/macOS (IANA)

            // Convert from UTC to target timezone
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            // Fallback: add +7 hours to UTC
            return DateTime.UtcNow.AddHours(7);
        }
    }
}
