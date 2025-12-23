using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BookingCare.Services.AI.Models.DTOs.Insights;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Partial class chứa các phương thức phân tích sâu từ Reason, Symptoms, IsRescheduled
/// </summary>
public partial class AiInsightsService
{
    /// <summary>
    /// Phân tích lý do đặt lịch từ trường Reason
    /// </summary>
    private async Task<List<ReasonAnalysis>> GetReasonAnalysesAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT Reason
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end 
  AND Reason IS NOT NULL 
  AND LEN(LTRIM(RTRIM(Reason))) > 0
  AND Status != 'CANCELLED'"; // Chỉ lấy lịch không bị hủy

            if (doctorId.HasValue)
            {
                sql += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            if (doctorId.HasValue)
            {
                command.Parameters.AddWithValue("@doctorId", doctorId.Value);
            }
            if (hospitalId.HasValue)
            {
                command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
            }

            var reasons = new List<string>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var reason = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    reasons.Add(reason.Trim().ToLowerInvariant());
                }
            }

            // Phân loại lý do dựa trên từ khóa
            return CategorizeReasons(reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing reasons: {Message}", ex.Message);
            return new List<ReasonAnalysis>();
        }
    }

    /// <summary>
    /// Phân loại lý do đặt lịch dựa trên từ khóa
    /// </summary>
    private static List<ReasonAnalysis> CategorizeReasons(List<string> reasons)
    {
        if (reasons.Count == 0)
        {
            return new List<ReasonAnalysis>();
        }

        var categories = new Dictionary<string, (int Count, List<string> Keywords)>
        {
            { "Khám bệnh lần đầu", (0, new List<string>()) },
            { "Tái khám", (0, new List<string>()) },
            { "Khám định kỳ", (0, new List<string>()) },
            { "Khám sức khỏe tổng quát", (0, new List<string>()) },
            { "Xét nghiệm", (0, new List<string>()) },
            { "Tư vấn", (0, new List<string>()) },
            { "Khác", (0, new List<string>()) }
        };

        // Từ khóa để phân loại
        var keywordMap = new Dictionary<string, List<string>>
        {
            { "Khám bệnh lần đầu", new List<string> { "lần đầu", "khám lần đầu", "chưa khám", "mới khám" } },
            { "Tái khám", new List<string> { "tái khám", "khám lại", "theo dõi", "kiểm tra lại", "tái" } },
            { "Khám định kỳ", new List<string> { "định kỳ", "định kì", "hàng tháng", "hàng quý", "hàng năm" } },
            { "Khám sức khỏe tổng quát", new List<string> { "tổng quát", "sức khỏe", "toàn diện", "check up" } },
            { "Xét nghiệm", new List<string> { "xét nghiệm", "test", "kiểm tra", "chẩn đoán" } },
            { "Tư vấn", new List<string> { "tư vấn", "hỏi", "tham khảo", "ý kiến" } }
        };

        foreach (var reason in reasons)
        {
            var categorized = false;
            foreach (var (category, keywords) in keywordMap)
            {
                if (keywords.Any(kw => reason.Contains(kw)))
                {
                    categories[category] = (categories[category].Count + 1, categories[category].Keywords);
                    // Lưu từ khóa phổ biến (lấy 3-5 từ đầu tiên)
                    var words = reason.Split(' ').Take(5).ToList();
                    categories[category].Keywords.AddRange(words);
                    categorized = true;
                    break;
                }
            }

            if (!categorized)
            {
                categories["Khác"] = (categories["Khác"].Count + 1, categories["Khác"].Keywords);
            }
        }

        var total = reasons.Count;
        return categories
            .Where(c => c.Value.Count > 0)
            .Select(c => new ReasonAnalysis
            {
                ReasonCategory = c.Key,
                Count = c.Value.Count,
                Percentage = Math.Round((double)c.Value.Count / total * 100, 2),
                CommonKeywords = c.Value.Keywords.GroupBy(k => k).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key).ToList()
            })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    /// <summary>
    /// Phân tích triệu chứng từ trường Symptoms
    /// </summary>
    private async Task<List<SymptomAnalysis>> GetSymptomAnalysesAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT Symptoms, SpecialtyId
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end 
  AND Symptoms IS NOT NULL 
  AND LEN(LTRIM(RTRIM(Symptoms))) > 0
  AND Status != 'CANCELLED'";

            if (doctorId.HasValue)
            {
                sql += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            if (doctorId.HasValue)
            {
                command.Parameters.AddWithValue("@doctorId", doctorId.Value);
            }
            if (hospitalId.HasValue)
            {
                command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
            }

            var symptomsData = new List<(string Symptoms, Guid? SpecialtyId)>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var symptoms = reader.GetString(0);
                var specialtyId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
                if (!string.IsNullOrWhiteSpace(symptoms))
                {
                    symptomsData.Add((symptoms.Trim().ToLowerInvariant(), specialtyId));
                }
            }

            // Phân loại triệu chứng
            return await CategorizeSymptoms(symptomsData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Message}", ex.Message);
            return new List<SymptomAnalysis>();
        }
    }

    /// <summary>
    /// Phân loại triệu chứng dựa trên từ khóa
    /// </summary>
    private async Task<List<SymptomAnalysis>> CategorizeSymptoms(
        List<(string Symptoms, Guid? SpecialtyId)> symptomsData,
        CancellationToken cancellationToken)
    {
        if (symptomsData.Count == 0)
        {
            return new List<SymptomAnalysis>();
        }

        var categories = new Dictionary<string, (int Count, List<string> Symptoms, Guid? SpecialtyId)>
        {
            { "Đau đầu, chóng mặt", (0, new List<string>(), null) },
            { "Sốt, cảm cúm", (0, new List<string>(), null) },
            { "Đau bụng, tiêu hóa", (0, new List<string>(), null) },
            { "Ho, khó thở", (0, new List<string>(), null) },
            { "Đau ngực, tim mạch", (0, new List<string>(), null) },
            { "Đau khớp, cơ xương", (0, new List<string>(), null) },
            { "Da liễu", (0, new List<string>(), null) },
            { "Mắt", (0, new List<string>(), null) },
            { "Tai mũi họng", (0, new List<string>(), null) },
            { "Khác", (0, new List<string>(), null) }
        };

        var keywordMap = new Dictionary<string, List<string>>
        {
            { "Đau đầu, chóng mặt", new List<string> { "đau đầu", "chóng mặt", "hoa mắt", "đau nửa đầu", "migraine" } },
            { "Sốt, cảm cúm", new List<string> { "sốt", "cảm", "cúm", "nhiệt", "ớn lạnh" } },
            { "Đau bụng, tiêu hóa", new List<string> { "đau bụng", "tiêu hóa", "tiêu chảy", "táo bón", "đầy hơi", "khó tiêu" } },
            { "Ho, khó thở", new List<string> { "ho", "khó thở", "thở", "hen", "viêm phổi" } },
            { "Đau ngực, tim mạch", new List<string> { "đau ngực", "tim", "huyết áp", "mạch", "trái tim" } },
            { "Đau khớp, cơ xương", new List<string> { "đau khớp", "đau lưng", "đau cột sống", "gãy", "xương", "cơ" } },
            { "Da liễu", new List<string> { "da", "ngứa", "mụn", "nổi mẩn", "dị ứng da" } },
            { "Mắt", new List<string> { "mắt", "nhìn", "mờ", "cận", "viễn" } },
            { "Tai mũi họng", new List<string> { "tai", "mũi", "họng", "viêm amidan", "viêm xoang" } }
        };

        foreach (var (symptoms, specialtyId) in symptomsData)
        {
            var categorized = false;
            foreach (var (category, keywords) in keywordMap)
            {
                if (keywords.Any(kw => symptoms.Contains(kw)))
                {
                    var current = categories[category];
                    categories[category] = (current.Count + 1, current.Symptoms, specialtyId ?? current.SpecialtyId);
                    current.Symptoms.Add(symptoms);
                    categorized = true;
                    break;
                }
            }

            if (!categorized)
            {
                var current = categories["Khác"];
                categories["Khác"] = (current.Count + 1, current.Symptoms, specialtyId ?? current.SpecialtyId);
            }
        }

        var total = symptomsData.Count;
        var result = new List<SymptomAnalysis>();

        foreach (var (category, (count, symptoms, specialtyId)) in categories.Where(c => c.Value.Count > 0))
        {
            string? specialtyName = null;
            if (specialtyId.HasValue)
            {
                specialtyName = await ResolveSpecialtyNameAsync(specialtyId, cancellationToken);
            }

            result.Add(new SymptomAnalysis
            {
                SymptomCategory = category,
                Count = count,
                Percentage = Math.Round((double)count / total * 100, 2),
                CommonSymptoms = symptoms.GroupBy(s => s).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key).ToList(),
                RelatedSpecialty = specialtyName
            });
        }

        return result.OrderByDescending(s => s.Count).ToList();
    }

    /// <summary>
    /// Phân tích lịch hẹn bị dời/reschedule
    /// </summary>
    private async Task<RescheduleInsight> GetRescheduleInsightAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT 
    COUNT(CASE WHEN IsRescheduled = 1 THEN 1 END) as TotalRescheduled,
    COUNT(CASE WHEN IsRescheduled = 1 AND Status = 'COMPLETED' THEN 1 END) as RescheduledCompleted,
    COUNT(CASE WHEN IsRescheduled = 1 AND Status = 'CANCELLED' THEN 1 END) as RescheduledCancelled,
    COUNT(1) as Total
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end";

            if (doctorId.HasValue)
            {
                sql += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            if (doctorId.HasValue)
            {
                command.Parameters.AddWithValue("@doctorId", doctorId.Value);
            }
            if (hospitalId.HasValue)
            {
                command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
            }

            int totalRescheduled = 0;
            int rescheduledCompleted = 0;
            int rescheduledCancelled = 0;
            int total = 0;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                totalRescheduled = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                rescheduledCompleted = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                rescheduledCancelled = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                total = reader.GetInt32(3);
            }

            var rescheduleRate = total == 0 ? 0 : Math.Round((double)totalRescheduled / total * 100, 2);
            var completionRate = totalRescheduled == 0 ? 0 : Math.Round((double)rescheduledCompleted / totalRescheduled * 100, 2);

            // Lấy patterns
            var patterns = await GetReschedulePatternsAsync(connection, start, end, cancellationToken, doctorId, hospitalId);

            return new RescheduleInsight
            {
                TotalRescheduled = totalRescheduled,
                RescheduleRate = rescheduleRate,
                RescheduledAndCompleted = rescheduledCompleted,
                RescheduledAndCancelled = rescheduledCancelled,
                RescheduledCompletionRate = completionRate,
                Patterns = patterns
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing reschedule insight: {Message}", ex.Message);
            return new RescheduleInsight();
        }
    }

    /// <summary>
    /// Lấy patterns của reschedule (theo bác sĩ, chuyên khoa, giờ)
    /// </summary>
    private async Task<List<ReschedulePattern>> GetReschedulePatternsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var patterns = new List<ReschedulePattern>();

        try
        {
            // Pattern by doctor (nếu không filter theo doctorId)
            if (!doctorId.HasValue)
            {
                var sqlDoctor = @"
SELECT TOP 5 DoctorId, 
    COUNT(CASE WHEN IsRescheduled = 1 THEN 1 END) as RescheduledCount,
    COUNT(1) as TotalCount
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND DoctorId IS NOT NULL";

                if (hospitalId.HasValue)
                {
                    sqlDoctor += " AND HospitalId = @hospitalId";
                }

                sqlDoctor += @"
GROUP BY DoctorId
HAVING COUNT(CASE WHEN IsRescheduled = 1 THEN 1 END) > 0
ORDER BY RescheduledCount DESC";

                using var cmdDoctor = new SqlCommand(sqlDoctor, connection);
                cmdDoctor.Parameters.AddWithValue("@start", start);
                cmdDoctor.Parameters.AddWithValue("@end", end);
                if (hospitalId.HasValue)
                {
                    cmdDoctor.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
                }

                using var readerDoctor = await cmdDoctor.ExecuteReaderAsync(cancellationToken);
                while (await readerDoctor.ReadAsync(cancellationToken))
                {
                    var docId = readerDoctor.GetGuid(0);
                    var rescheduleCount = readerDoctor.GetInt32(1);
                    var totalCount = readerDoctor.GetInt32(2);
                    var rate = totalCount == 0 ? 0 : Math.Round((double)rescheduleCount / totalCount * 100, 2);

                    var doctorName = await ResolveDoctorNameAsync(docId, cancellationToken);

                    patterns.Add(new ReschedulePattern
                    {
                        PatternType = "by_doctor",
                        PatternValue = doctorName,
                        RescheduleCount = rescheduleCount,
                        RescheduleRate = rate
                    });
                }
            }

            // Pattern by specialty
            var sqlSpecialty = @"
SELECT TOP 5 SpecialtyId, 
    COUNT(CASE WHEN IsRescheduled = 1 THEN 1 END) as RescheduledCount,
    COUNT(1) as TotalCount
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND SpecialtyId IS NOT NULL";

            if (doctorId.HasValue)
            {
                sqlSpecialty += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sqlSpecialty += " AND HospitalId = @hospitalId";
            }

            sqlSpecialty += @"
GROUP BY SpecialtyId
HAVING COUNT(CASE WHEN IsRescheduled = 1 THEN 1 END) > 0
ORDER BY RescheduledCount DESC";

            using (var cmdSpecialty = new SqlCommand(sqlSpecialty, connection))
            {
                cmdSpecialty.Parameters.AddWithValue("@start", start);
                cmdSpecialty.Parameters.AddWithValue("@end", end);
                if (doctorId.HasValue)
                {
                    cmdSpecialty.Parameters.AddWithValue("@doctorId", doctorId.Value);
                }
                if (hospitalId.HasValue)
                {
                    cmdSpecialty.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
                }

                using var readerSpecialty = await cmdSpecialty.ExecuteReaderAsync(cancellationToken);
                while (await readerSpecialty.ReadAsync(cancellationToken))
                {
                    var specId = readerSpecialty.GetGuid(0);
                    var rescheduleCount = readerSpecialty.GetInt32(1);
                    var totalCount = readerSpecialty.GetInt32(2);
                    var rate = totalCount == 0 ? 0 : Math.Round((double)rescheduleCount / totalCount * 100, 2);

                    var specialtyName = await ResolveSpecialtyNameAsync(specId, cancellationToken);

                    patterns.Add(new ReschedulePattern
                    {
                        PatternType = "by_specialty",
                        PatternValue = specialtyName ?? $"Chuyên khoa {specId}",
                        RescheduleCount = rescheduleCount,
                        RescheduleRate = rate
                    });
                }
            }

            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reschedule patterns: {Message}", ex.Message);
            return patterns;
        }
    }

    /// <summary>
    /// Phân tích chi tiết lý do hủy lịch từ trường Reason khi Status = CANCELLED
    /// </summary>
    private async Task<DetailedCancellationAnalysis> GetDetailedCancellationAnalysisAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT Reason, AppointmentTimeId, SpecialtyId
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end 
  AND Status = 'CANCELLED'";

            if (doctorId.HasValue)
            {
                sql += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            if (doctorId.HasValue)
            {
                command.Parameters.AddWithValue("@doctorId", doctorId.Value);
            }
            if (hospitalId.HasValue)
            {
                command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
            }

            var cancellationData = new List<(string? Reason, int TimeSlotId, Guid? SpecialtyId)>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var reason = reader.IsDBNull(0) ? null : reader.GetString(0);
                var timeSlotId = reader.GetInt32(1);
                var specialtyId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
                cancellationData.Add((reason, timeSlotId, specialtyId));
            }

            // Phân loại lý do hủy
            var topReasons = CategorizeCancellationReasons(cancellationData.Select(d => d.Reason).Where(r => !string.IsNullOrWhiteSpace(r)).ToList()!);

            // Phân tích theo khung giờ
            var reasonsByTimeSlot = new Dictionary<string, int>();
            foreach (var (reason, timeSlotId, _) in cancellationData.Where(d => !string.IsNullOrWhiteSpace(d.Reason)))
            {
                var timeLabel = MapTimeSlotToLabel(timeSlotId);
                if (!reasonsByTimeSlot.ContainsKey(timeLabel))
                {
                    reasonsByTimeSlot[timeLabel] = 0;
                }
                reasonsByTimeSlot[timeLabel]++;
            }

            // Phân tích theo chuyên khoa
            var reasonsBySpecialty = new Dictionary<string, int>();
            foreach (var (reason, _, specialtyId) in cancellationData.Where(d => !string.IsNullOrWhiteSpace(d.Reason) && d.SpecialtyId.HasValue))
            {
                var specialtyName = await ResolveSpecialtyNameAsync(specialtyId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(specialtyName))
                {
                    if (!reasonsBySpecialty.ContainsKey(specialtyName))
                    {
                        reasonsBySpecialty[specialtyName] = 0;
                    }
                    reasonsBySpecialty[specialtyName]++;
                }
            }

            var cancellationsWithReason = cancellationData.Count(d => !string.IsNullOrWhiteSpace(d.Reason));
            var cancellationsWithoutReason = cancellationData.Count - cancellationsWithReason;

            return new DetailedCancellationAnalysis
            {
                TopReasons = topReasons,
                ReasonsByTimeSlot = reasonsByTimeSlot,
                ReasonsBySpecialty = reasonsBySpecialty,
                CancellationsWithReason = cancellationsWithReason,
                CancellationsWithoutReason = cancellationsWithoutReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing detailed cancellation: {Message}", ex.Message);
            return new DetailedCancellationAnalysis();
        }
    }

    /// <summary>
    /// Phân loại lý do hủy lịch dựa trên từ khóa
    /// </summary>
    private static List<CancellationReasonDetail> CategorizeCancellationReasons(List<string> reasons)
    {
        if (reasons.Count == 0)
        {
            return new List<CancellationReasonDetail>();
        }

        var categories = new Dictionary<string, (int Count, List<string> Keywords, string? RecommendedAction)>
        {
            { "Bận việc đột xuất", (0, new List<string>(), "Gửi tin nhắc nhở 24h trước, cho phép dời lịch dễ dàng qua app") },
            { "Đổi ý, không muốn khám nữa", (0, new List<string>(), "Tăng cường tư vấn trước khi đặt lịch, cải thiện chất lượng dịch vụ") },
            { "Không liên lạc được với bệnh viện/bác sĩ", (0, new List<string>(), "Cải thiện hệ thống liên lạc, gửi thông tin rõ ràng sau khi đặt lịch") },
            { "Tìm được bác sĩ/bệnh viện khác", (0, new List<string>(), "Cải thiện chất lượng dịch vụ, giá cả cạnh tranh, tăng uy tín") },
            { "Lý do sức khỏe", (0, new List<string>(), "Hỗ trợ dời lịch linh hoạt, tư vấn chăm sóc sức khỏe") },
            { "Lý do tài chính", (0, new List<string>(), "Cung cấp thông tin chi phí rõ ràng trước, hỗ trợ thanh toán linh hoạt") },
            { "Vấn đề về lịch hẹn/bác sĩ", (0, new List<string>(), "Tăng tính linh hoạt trong đặt lịch, cải thiện thông tin về bác sĩ") },
            { "Khác", (0, new List<string>(), "Khảo sát lý do hủy chi tiết hơn để cải thiện dịch vụ") }
        };

        var keywordMap = new Dictionary<string, List<string>>
        {
            { "Bận việc đột xuất", new List<string> { "bận", "việc", "đột xuất", "không thể", "không đi được", "có việc", "công tác", "họp", "gia đình", "chăm sóc" } },
            { "Đổi ý, không muốn khám nữa", new List<string> { "đổi ý", "không muốn", "thôi không", "hủy", "không cần", "cảm thấy không cần", "quyết định không" } },
            { "Không liên lạc được với bệnh viện/bác sĩ", new List<string> { "không liên lạc", "không gọi được", "không trả lời", "không phản hồi", "không nhận được", "không rõ địa chỉ", "không rõ thông tin" } },
            { "Tìm được bác sĩ/bệnh viện khác", new List<string> { "tìm được", "bác sĩ khác", "bệnh viện khác", "chuyển", "đổi bác sĩ", "gần nhà", "giới thiệu", "chuyên môn cao", "thuận tiện", "chi phí phù hợp" } },
            { "Lý do sức khỏe", new List<string> { "sức khỏe", "bệnh", "không khỏe", "ốm", "nặng hơn", "nhẹ hơn", "khỏi bệnh", "thuyên giảm", "tự điều trị", "nghỉ ngơi", "uống thuốc" } },
            { "Lý do tài chính", new List<string> { "tiền", "chi phí", "đắt", "không đủ", "tài chính", "cao hơn", "cần cân nhắc", "tiết kiệm" } },
            { "Vấn đề về lịch hẹn/bác sĩ", new List<string> { "khung giờ", "không phù hợp", "lịch làm việc", "sắp xếp", "thời gian", "bác sĩ không có lịch", "đổi bác sĩ", "chờ quá lâu", "không thể chờ" } }
        };

        foreach (var reason in reasons)
        {
            var reasonLower = reason.ToLowerInvariant();
            var categorized = false;
            foreach (var (category, keywords) in keywordMap)
            {
                if (keywords.Any(kw => reasonLower.Contains(kw)))
                {
                    var current = categories[category];
                    categories[category] = (current.Count + 1, current.Keywords, current.RecommendedAction);
                    current.Keywords.Add(reason);
                    categorized = true;
                    break;
                }
            }

            if (!categorized)
            {
                var current = categories["Khác"];
                categories["Khác"] = (current.Count + 1, current.Keywords, current.RecommendedAction);
            }
        }

        var total = reasons.Count;
        return categories
            .Where(c => c.Value.Count > 0)
            .Select(c => new CancellationReasonDetail
            {
                ReasonCategory = c.Key,
                Count = c.Value.Count,
                Percentage = Math.Round((double)c.Value.Count / total * 100, 2),
                CommonKeywords = c.Value.Keywords.GroupBy(k => k).OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key).ToList(),
                RecommendedAction = c.Value.RecommendedAction
            })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    /// <summary>
    /// Map time slot ID to label
    /// </summary>
    private static string MapTimeSlotToLabel(int timeSlotId)
    {
        return timeSlotId switch
        {
            0 => "00:00-01:00",
            1 => "01:00-02:00",
            2 => "02:00-03:00",
            3 => "03:00-04:00",
            4 => "04:00-05:00",
            5 => "05:00-06:00",
            6 => "06:00-07:00",
            7 => "07:00-08:00",
            8 => "08:00-09:00",
            9 => "09:00-10:00",
            10 => "10:00-11:00",
            11 => "11:00-12:00",
            12 => "12:00-13:00",
            13 => "13:00-14:00",
            14 => "14:00-15:00",
            15 => "15:00-16:00",
            16 => "16:00-17:00",
            17 => "17:00-18:00",
            18 => "18:00-19:00",
            19 => "19:00-20:00",
            20 => "20:00-21:00",
            21 => "21:00-22:00",
            22 => "22:00-23:00",
            23 => "23:00-24:00",
            _ => $"Slot {timeSlotId}"
        };
    }
}
