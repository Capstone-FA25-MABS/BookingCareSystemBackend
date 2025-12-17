using System;
using System.Linq;
using System.Text.Json;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.DTOs.Insights;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.AI.Services.Implementations;

public class AiInsightsService : IAiInsightsService
{
    private readonly string _appointmentConnection;
    private readonly string? _doctorConnection;
    private readonly string? _hospitalConnection;
    private readonly GroqApiHelper _groqApiHelper;
    private readonly ServiceGroqConfiguration _groqConfig;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly HospitalService.HospitalServiceClient _hospitalClient;
    private readonly ILogger<AiInsightsService> _logger;

    public AiInsightsService(
        IConfiguration configuration,
        GroqApiHelper groqApiHelper,
        IOptions<GroqServicesConfiguration> groqOptions,
        DoctorService.DoctorServiceClient doctorClient,
        HospitalService.HospitalServiceClient hospitalClient,
        ILogger<AiInsightsService> logger)
    {
        _appointmentConnection = configuration.GetConnectionString("AppointmentConnection")
            ?? throw new InvalidOperationException("AppointmentConnection is not configured");
        _doctorConnection = configuration.GetConnectionString("DoctorConnection");
        _hospitalConnection = configuration.GetConnectionString("HospitalConnection");
        _groqApiHelper = groqApiHelper;
        _groqConfig = groqOptions.Value.InsightsSummary ?? new ServiceGroqConfiguration();
        _doctorClient = doctorClient;
        _hospitalClient = hospitalClient;
        _logger = logger;
    }

    public async Task<AiInsightResponse> GenerateAsync(
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime currentStart, currentEnd, previousStart, previousEnd;
            string periodLabel;

            // Nếu có fromDate và toDate, dùng date range tùy chỉnh
            if (request?.FromDate.HasValue == true && request?.ToDate.HasValue == true)
            {
                currentStart = request.FromDate.Value.Date;
                currentEnd = request.ToDate.Value.Date.AddDays(1).AddTicks(-1); // End of day

                // Tính previous period: cùng độ dài, trước đó
                var periodLength = (currentEnd - currentStart).TotalDays;
                previousEnd = currentStart.AddTicks(-1);
                previousStart = previousEnd.AddDays(-periodLength).Date;

                periodLabel = $"Tùy chỉnh ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd})";
            }
            else
            {
                // Fallback về period mặc định
                var normalizedPeriod = (request?.Period ?? "week").Trim().ToLowerInvariant();
                var (CurrentStart, CurrentEnd, PreviousStart, PreviousEnd, Label) = ResolvePeriod(normalizedPeriod);
                currentStart = CurrentStart;
                currentEnd = CurrentEnd;
                previousStart = PreviousStart;
                previousEnd = PreviousEnd;
                periodLabel = Label;
            }

            _logger.LogInformation("Collecting metrics for period {Period} ({Start} to {End})",
                periodLabel, currentStart, currentEnd);

            var metrics = await CollectMetricsAsync(
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                cancellationToken);

            _logger.LogInformation("Metrics collected. Total appointments: {Total}", metrics.CurrentTotal);

            var prompt = BuildPrompt(
                metrics,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                periodLabel);

            _logger.LogInformation("Calling Groq API with model {Model}, maxTokens: {MaxTokens}",
                _groqConfig.PrimaryModel, _groqConfig.MaxTokens ?? 6000);

            var summary = await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _groqConfig,
                temperature: _groqConfig.Temperature ?? 0.2,
                maxTokens: _groqConfig.MaxTokens ?? 6000,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Groq API response received. Summary length: {Length}", summary?.Length ?? 0);

            // Parse structured data from AI response
            var (predictions, alerts, rootCauses, analysisConclusion, predictionConclusion) = ParseStructuredData(summary ?? string.Empty, metrics, _logger);

            return new AiInsightResponse
            {
                Summary = summary?.Trim() ?? string.Empty,
                Period = periodLabel,
                PeriodStart = currentStart,
                PeriodEnd = currentEnd,
                GeneratedAt = DateTime.UtcNow,
                Model = _groqConfig.PrimaryModel,
                Metrics = metrics,
                Predictions = predictions,
                Alerts = alerts,
                RootCauseAnalyses = rootCauses,
                AnalysisConclusion = analysisConclusion,
                PredictionConclusion = predictionConclusion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights: {Message}", ex.Message);
            throw;
        }
    }

    private static (DateTime CurrentStart, DateTime CurrentEnd, DateTime PreviousStart, DateTime PreviousEnd, string Label) ResolvePeriod(
        string period)
    {
        var today = DateTime.UtcNow.Date;
        if (period == "month")
        {
            var firstDay = new DateTime(today.Year, today.Month, 1);
            var currentStart = firstDay;
            var currentEnd = firstDay.AddMonths(1).AddDays(-1).AddDays(1).AddSeconds(-1);
            var previousStart = firstDay.AddMonths(-1);
            var previousEnd = firstDay.AddDays(-1).AddDays(1).AddSeconds(-1);
            return (currentStart, currentEnd, previousStart, previousEnd, "month");
        }

        var currentWeekStart = StartOfWeek(today);
        var currentWeekEnd = currentWeekStart.AddDays(7).AddSeconds(-1);
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var previousWeekEnd = currentWeekStart.AddSeconds(-1);
        return (currentWeekStart, currentWeekEnd, previousWeekStart, previousWeekEnd, "week");
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private async Task<AiInsightMetrics> CollectMetricsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(_appointmentConnection);
            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Collecting basic metrics...");
            var currentTotal = await CountAppointmentsAsync(connection, currentStart, currentEnd, null, cancellationToken);
            var previousTotal = await CountAppointmentsAsync(connection, previousStart, previousEnd, null, cancellationToken);
            var currentCancelled = await CountAppointmentsAsync(connection, currentStart, currentEnd, "CANCELLED", cancellationToken);
            var previousCancelled = await CountAppointmentsAsync(connection, previousStart, previousEnd, "CANCELLED", cancellationToken);
            var currentCompleted = await CountAppointmentsAsync(connection, currentStart, currentEnd, "COMPLETED", cancellationToken);

            _logger.LogInformation("Getting top specialty...");
            var topSpecialty = await GetTopSpecialtyAsync(connection, currentStart, currentEnd, cancellationToken);
            var topSpecialtyName = await ResolveSpecialtyNameAsync(topSpecialty?.SpecialtyId, cancellationToken);

            // Extended analytics - run sequentially to avoid connection conflicts
            _logger.LogInformation("Collecting extended analytics...");
            var topDoctors = await GetTopDoctorsAsync(connection, currentStart, currentEnd, cancellationToken);
            var topHospitals = await GetTopHospitalsAsync(connection, currentStart, currentEnd, cancellationToken);
            var peakHours = await GetPeakHoursAsync(connection, currentStart, currentEnd, cancellationToken);
            var specialtyBreakdown = await GetSpecialtyBreakdownAsync(connection, currentStart, currentEnd, previousStart, previousEnd, cancellationToken);
            var cancellationDetails = await GetCancellationDetailsAsync(connection, currentStart, currentEnd, cancellationToken);
            var patientSegments = await GetPatientSegmentsAsync(connection, currentStart, currentEnd, cancellationToken);

            return new AiInsightMetrics
            {
                CurrentTotal = currentTotal,
                PreviousTotal = previousTotal,
                GrowthPercent = CalculateDeltaPercent(currentTotal, previousTotal),
                CurrentCompleted = currentCompleted,
                CurrentCancelled = currentCancelled,
                CancellationRate = currentTotal == 0 ? 0 : Math.Round((double)currentCancelled / currentTotal * 100, 2),
                CancellationDeltaPercent = CalculateDeltaPercent(currentCancelled, previousCancelled),
                TopSpecialtyId = topSpecialty?.SpecialtyId,
                TopSpecialtyName = topSpecialtyName,
                TopSpecialtyCount = topSpecialty?.Count ?? 0,
                TopSpecialtyShare = currentTotal == 0
                    ? 0
                    : Math.Round((double)(topSpecialty?.Count ?? 0) / currentTotal * 100, 2),
                TopDoctors = topDoctors,
                TopHospitals = topHospitals,
                PeakHours = peakHours,
                SpecialtyBreakdown = specialtyBreakdown,
                CancellationDetails = cancellationDetails,
                PatientSegments = patientSegments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics: {Message}", ex.Message);
            throw;
        }
    }

    private static async Task<int> CountAppointmentsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        string? status,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var sql = "SELECT COUNT(1) FROM Appointments WHERE AppointmentDate BETWEEN @start AND @end";
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND Status = @status";
        }
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
        if (!string.IsNullOrWhiteSpace(status))
        {
            command.Parameters.AddWithValue("@status", status);
        }
        if (doctorId.HasValue)
        {
            command.Parameters.AddWithValue("@doctorId", doctorId.Value);
        }
        if (hospitalId.HasValue)
        {
            command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0);
    }

    private sealed record SpecialtyCount(Guid SpecialtyId, int Count);

    private static async Task<SpecialtyCount?> GetTopSpecialtyAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var sql = @"
SELECT TOP 1 SpecialtyId, COUNT(1) as Cnt
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND SpecialtyId IS NOT NULL";
        
        if (doctorId.HasValue)
        {
            sql += " AND DoctorId = @doctorId";
        }
        if (hospitalId.HasValue)
        {
            sql += " AND HospitalId = @hospitalId";
        }
        
        sql += @"
GROUP BY SpecialtyId
ORDER BY Cnt DESC";

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

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var specialtyId = reader.GetGuid(0);
            var count = reader.GetInt32(1);
            return new SpecialtyCount(specialtyId, count);
        }

        return null;
    }

    private async Task<string?> ResolveSpecialtyNameAsync(Guid? specialtyId, CancellationToken cancellationToken)
    {
        if (!specialtyId.HasValue || specialtyId.Value == Guid.Empty)
        {
            return null;
        }

        try
        {
            var response = await _doctorClient.GetSpecialtyByIdAsync(
                new GetSpecialtyByIdRequest { Id = specialtyId.Value.ToString() },
                cancellationToken: cancellationToken);

            return string.IsNullOrWhiteSpace(response?.Name) ? null : response.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy tên chuyên khoa {SpecialtyId}", specialtyId);
            return null;
        }
    }

    private async Task<string> ResolveDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _doctorClient.GetDoctorAsync(
                new GetDoctorRequest { Id = doctorId.ToString() },
                cancellationToken: cancellationToken);

            if (response != null && !string.IsNullOrWhiteSpace(response.FirstName))
            {
                var fullName = $"{response.FirstName} {response.LastName}".Trim();
                return string.IsNullOrWhiteSpace(fullName) ? $"Bác sĩ {doctorId}" : fullName;
            }

            return $"Bác sĩ {doctorId}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy tên bác sĩ {DoctorId}", doctorId);
            return $"Bác sĩ {doctorId}";
        }
    }

    private async Task<string> ResolveHospitalNameAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _hospitalClient.GetHospitalAsync(
                new GetHospitalRequest { Id = hospitalId.ToString() },
                cancellationToken: cancellationToken);

            if (response != null && !string.IsNullOrWhiteSpace(response.Name))
            {
                return response.Name;
            }

            return $"Bệnh viện {hospitalId}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy tên bệnh viện {HospitalId}", hospitalId);
            return $"Bệnh viện {hospitalId}";
        }
    }

    private static double CalculateDeltaPercent(int currentValue, int previousValue)
    {
        if (previousValue <= 0)
        {
            return currentValue > 0 ? 100 : 0;
        }

        return Math.Round(((double)(currentValue - previousValue) / previousValue) * 100, 2);
    }

    private async Task<List<DoctorUtilization>> GetTopDoctorsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT TOP 5 DoctorId, COUNT(1) as Cnt
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND DoctorId IS NOT NULL";
            
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }
            
            sql += @"
GROUP BY DoctorId
ORDER BY Cnt DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            if (hospitalId.HasValue)
            {
                command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
            }

            var doctorIds = new List<(Guid DoctorId, int Count)>();
            var total = await CountAppointmentsAsync(connection, start, end, null, cancellationToken, hospitalId: hospitalId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var doctorId = reader.GetGuid(0);
                var count = reader.GetInt32(1);
                doctorIds.Add((doctorId, count));
            }

            // Resolve doctor names after closing reader
            var doctors = new List<DoctorUtilization>();
            foreach (var (doctorId, count) in doctorIds)
            {
                var utilization = total == 0 ? 0 : Math.Round((double)count / total * 100, 2);

                // Try to get doctor name (optional, can be null)
                string? doctorName = null;
                try
                {
                    var doctorResponse = await _doctorClient.GetDoctorAsync(
                        new GetDoctorRequest { Id = doctorId.ToString() },
                        cancellationToken: cancellationToken);
                    doctorName = doctorResponse?.FullName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get doctor name for {DoctorId}", doctorId);
                    // Ignore if can't get doctor name
                }

                doctors.Add(new DoctorUtilization
                {
                    DoctorId = doctorId,
                    DoctorName = doctorName,
                    AppointmentCount = count,
                    UtilizationPercent = utilization
                });
            }

            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top doctors: {Message}", ex.Message);
            return new List<DoctorUtilization>();
        }
    }

    private async Task<List<HospitalStats>> GetTopHospitalsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP 5 HospitalId, COUNT(1) as Cnt
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND HospitalId IS NOT NULL
GROUP BY HospitalId
ORDER BY Cnt DESC";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@start", start);
        command.Parameters.AddWithValue("@end", end);

        var hospitalIds = new List<(Guid HospitalId, int Count)>();
        var total = await CountAppointmentsAsync(connection, start, end, null, cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hospitalId = reader.GetGuid(0);
            var count = reader.GetInt32(1);
            hospitalIds.Add((hospitalId, count));
        }

        // Resolve hospital names after closing reader
        var hospitals = new List<HospitalStats>();
        foreach (var (hospitalId, count) in hospitalIds)
        {
            var share = total == 0 ? 0 : Math.Round((double)count / total * 100, 2);

            // Try to get hospital name (optional, can be null)
            string? hospitalName = null;
            try
            {
                var hospitalResponse = await _hospitalClient.GetHospitalAsync(
                    new GetHospitalRequest { Id = hospitalId.ToString() },
                    cancellationToken: cancellationToken);
                hospitalName = hospitalResponse?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get hospital name for {HospitalId}", hospitalId);
                // Ignore if can't get hospital name
            }

            hospitals.Add(new HospitalStats
            {
                HospitalId = hospitalId,
                HospitalName = hospitalName,
                AppointmentCount = count,
                SharePercent = share
            });
        }

        return hospitals;
    }

    private async Task<List<PeakHourStats>> GetPeakHoursAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var sql = @"
SELECT AppointmentTimeId, COUNT(1) as Cnt
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND AppointmentTimeId IS NOT NULL";
        
        if (doctorId.HasValue)
        {
            sql += " AND DoctorId = @doctorId";
        }
        if (hospitalId.HasValue)
        {
            sql += " AND HospitalId = @hospitalId";
        }
        
        sql += @"
GROUP BY AppointmentTimeId
ORDER BY Cnt DESC";

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

        var peakHours = new List<PeakHourStats>();
        var total = await CountAppointmentsAsync(connection, start, end, null, cancellationToken, doctorId: doctorId, hospitalId: hospitalId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var timeSlotId = reader.GetInt32(0);
            var count = reader.GetInt32(1);
            var share = total == 0 ? 0 : Math.Round((double)count / total * 100, 2);

            // Map time slot ID to label (0-23 for hours, or custom mapping)
            var timeLabel = timeSlotId switch
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

            peakHours.Add(new PeakHourStats
            {
                TimeSlotId = timeSlotId,
                TimeSlotLabel = timeLabel,
                AppointmentCount = count,
                SharePercent = share
            });
        }

        return peakHours.OrderByDescending(h => h.AppointmentCount).Take(5).ToList();
    }

    private async Task<List<SpecialtyStats>> GetSpecialtyBreakdownAsync(
        SqlConnection connection,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            var sql = @"
SELECT SpecialtyId, COUNT(1) as Cnt
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND SpecialtyId IS NOT NULL";
            
            if (doctorId.HasValue)
            {
                sql += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sql += " AND HospitalId = @hospitalId";
            }
            
            sql += @"
GROUP BY SpecialtyId
ORDER BY Cnt DESC";

            var currentTotal = await CountAppointmentsAsync(connection, currentStart, currentEnd, null, cancellationToken, doctorId: doctorId, hospitalId: hospitalId);

            // First, collect all specialty data from reader
            var specialtyData = new List<(Guid SpecialtyId, int CurrentCount)>();

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@start", currentStart);
                command.Parameters.AddWithValue("@end", currentEnd);
                if (doctorId.HasValue)
                {
                    command.Parameters.AddWithValue("@doctorId", doctorId.Value);
                }
                if (hospitalId.HasValue)
                {
                    command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
                }

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var specialtyId = reader.GetGuid(0);
                    var currentCount = reader.GetInt32(1);
                    specialtyData.Add((specialtyId, currentCount));
                }
            } // Reader is closed here

            // Now process the data and call other methods
            var specialties = new List<SpecialtyStats>();
            foreach (var (specialtyId, currentCount) in specialtyData)
            {
                var share = currentTotal == 0 ? 0 : Math.Round((double)currentCount / currentTotal * 100, 2);

                // Get previous period count (reader is closed, so this is safe)
                var previousCount = await CountAppointmentsBySpecialtyAsync(connection, previousStart, previousEnd, specialtyId, cancellationToken, doctorId: doctorId, hospitalId: hospitalId);
                var growth = CalculateDeltaPercent(currentCount, previousCount);

                var specialtyName = await ResolveSpecialtyNameAsync(specialtyId, cancellationToken);

                specialties.Add(new SpecialtyStats
                {
                    SpecialtyId = specialtyId,
                    SpecialtyName = specialtyName,
                    AppointmentCount = currentCount,
                    SharePercent = share,
                    GrowthPercent = growth
                });
            }

            return specialties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting specialty breakdown: {Message}", ex.Message);
            return new List<SpecialtyStats>();
        }
    }

    private static async Task<int> CountAppointmentsBySpecialtyAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        Guid specialtyId,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var sql = @"
SELECT COUNT(1) FROM Appointments 
WHERE AppointmentDate BETWEEN @start AND @end AND SpecialtyId = @specialtyId";
        
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
        command.Parameters.AddWithValue("@specialtyId", specialtyId);
        if (doctorId.HasValue)
        {
            command.Parameters.AddWithValue("@doctorId", doctorId.Value);
        }
        if (hospitalId.HasValue)
        {
            command.Parameters.AddWithValue("@hospitalId", hospitalId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0);
    }

    private async Task<CancellationInsight> GetCancellationDetailsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        var sql = @"
SELECT 
    COUNT(1) as TotalCancelled,
    SUM(CASE WHEN CancelledBy = 'PATIENT' THEN 1 ELSE 0 END) as CancelledByPatient,
    SUM(CASE WHEN CancelledBy = 'SYSTEM' OR CancelledBy = 'ADMIN' THEN 1 ELSE 0 END) as CancelledBySystem
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end AND Status = 'CANCELLED'";
        
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

        var total = await CountAppointmentsAsync(connection, start, end, null, cancellationToken, doctorId: doctorId, hospitalId: hospitalId);
        var totalCancelled = await CountAppointmentsAsync(connection, start, end, "CANCELLED", cancellationToken, doctorId: doctorId, hospitalId: hospitalId);
        var cancellationRate = total == 0 ? 0 : Math.Round((double)totalCancelled / total * 100, 2);

        int cancelledByPatient = 0;
        int cancelledBySystem = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            cancelledByPatient = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            cancelledBySystem = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
        }

        return new CancellationInsight
        {
            TotalCancelled = totalCancelled,
            CancelledByPatient = cancelledByPatient,
            CancelledBySystem = cancelledBySystem,
            CancellationRate = cancellationRate,
            CancellationReasons = new Dictionary<string, int>
            {
                { "PATIENT", cancelledByPatient },
                { "SYSTEM", cancelledBySystem }
            }
        };
    }

    private async Task<PatientSegmentStats> GetPatientSegmentsAsync(
        SqlConnection connection,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken,
        Guid? doctorId = null,
        Guid? hospitalId = null)
    {
        try
        {
            // New patients: those who have their first appointment in this period
            // Returning patients: those who have appointments before this period
            var sqlNewPatients = @"
SELECT COUNT(DISTINCT PatientAccountId) as NewPatients
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end";
            
            if (doctorId.HasValue)
            {
                sqlNewPatients += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sqlNewPatients += " AND HospitalId = @hospitalId";
            }
            
            sqlNewPatients += @"
  AND PatientAccountId IN (
      SELECT PatientAccountId
      FROM Appointments";
            
            if (doctorId.HasValue)
            {
                sqlNewPatients += " WHERE DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sqlNewPatients += doctorId.HasValue ? " AND HospitalId = @hospitalId" : " WHERE HospitalId = @hospitalId";
            }
            
            sqlNewPatients += @"
      GROUP BY PatientAccountId
      HAVING MIN(AppointmentDate) BETWEEN @start AND @end
  )";

            var sqlReturningPatients = @"
SELECT COUNT(DISTINCT PatientAccountId) as ReturningPatients
FROM Appointments
WHERE AppointmentDate BETWEEN @start AND @end";
            
            if (doctorId.HasValue)
            {
                sqlReturningPatients += " AND DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sqlReturningPatients += " AND HospitalId = @hospitalId";
            }
            
            sqlReturningPatients += @"
  AND PatientAccountId IN (
      SELECT PatientAccountId
      FROM Appointments";
            
            if (doctorId.HasValue)
            {
                sqlReturningPatients += " WHERE DoctorId = @doctorId";
            }
            if (hospitalId.HasValue)
            {
                sqlReturningPatients += doctorId.HasValue ? " AND HospitalId = @hospitalId" : " WHERE HospitalId = @hospitalId";
            }
            
            sqlReturningPatients += @"
      GROUP BY PatientAccountId
      HAVING MIN(AppointmentDate) < @start
  )";

            int newPatients = 0;
            int returningPatients = 0;

            // Get new patients count
            using (var command = new SqlCommand(sqlNewPatients, connection))
            {
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
                var result = await command.ExecuteScalarAsync(cancellationToken);
                newPatients = Convert.ToInt32(result ?? 0);
            }

            // Get returning patients count
            using (var command = new SqlCommand(sqlReturningPatients, connection))
            {
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
                var result = await command.ExecuteScalarAsync(cancellationToken);
                returningPatients = Convert.ToInt32(result ?? 0);
            }

            var totalPatients = newPatients + returningPatients;

            return new PatientSegmentStats
            {
                NewPatients = newPatients,
                ReturningPatients = returningPatients,
                NewPatientRate = totalPatients == 0 ? 0 : Math.Round((double)newPatients / totalPatients * 100, 2),
                ReturningPatientRate = totalPatients == 0 ? 0 : Math.Round((double)returningPatients / totalPatients * 100, 2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient segments: {Message}", ex.Message);
            return new PatientSegmentStats();
        }
    }

    private static string BuildPrompt(
        AiInsightMetrics metrics,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        string periodLabel)
    {
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });

        return $@"
Bạn là chuyên gia phân tích dữ liệu BI cho BookingCare - hệ thống đặt lịch khám bệnh trực tuyến.
Phân tích dữ liệu và tạo báo cáo insights chuyên nghiệp bằng TIẾNG VIỆT cho ban quản trị.

⚠️ YÊU CẦU BẮT BUỘC:
- TẤT CẢ nội dung phải bằng TIẾNG VIỆT HOÀN TOÀN (kết luận, phân tích, dự đoán, tên chuyên khoa/bác sĩ/bệnh viện, khuyến nghị, cảnh báo)
- TUYỆT ĐỐI KHÔNG được dùng tiếng Trung, tiếng Anh, hoặc bất kỳ ngôn ngữ nào khác (trừ tên riêng)
- Sử dụng số liệu cụ thể từ dữ liệu, KHÔNG tạo dữ liệu giả
- Viết thành đoạn văn liền mạch, không phải bullet points
- Độ dài báo cáo: 1500-3000 từ

BỐI CẢNH:
- Giai đoạn hiện tại: {currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd} ({periodLabel})
- Giai đoạn so sánh: {previousStart:yyyy-MM-dd} → {previousEnd:yyyy-MM-dd}

DỮ LIỆU THỐNG KÊ:
{metricsJson}

CẤU TRÚC BÁO CÁO:

BƯỚC 1: PHÂN TÍCH VĂN BẢN (viết trước JSON):
Phân tích chi tiết các khía cạnh sau (mỗi phần 100-200 từ):
1. Nguyên nhân tăng/giảm lượt đặt (yếu tố: mùa vụ, sự kiện, chính sách, marketing)
2. Phân tích top bác sĩ (tên, số lượt, mức độ sử dụng, quá tải/thiếu việc)
3. Phân tích top bệnh viện (tên, số lượt, phân bổ, cân bằng)
4. Phân tích chuyên khoa (số lượt, %, tăng trưởng, xu hướng)
5. Phân tích khung giờ vàng (giờ nào nhiều lượt nhất, nhu cầu theo thời gian)
6. Phân tích khách hàng (tỷ lệ mới vs quay lại, retention)
7. Phân tích hủy lịch (tỷ lệ, lý do, pattern, tác động)
8. Đề xuất hành động (5-7 đề xuất cụ thể, có lý do và kết quả mong đợi)
9. Đánh giá rủi ro (3-5 rủi ro: mô tả, mức độ, khả năng, tác động, biện pháp)
10. Dự đoán tương lai (lượt đặt tuần/tháng tới, tỉ lệ hủy, chuyên khoa phổ biến, confidence, lý do)
11. Cảnh báo (chỉ số bất thường: type, title BẰNG TIẾNG VIỆT, message BẰNG TIẾNG VIỆT chi tiết, severity, metric BẰNG TIẾNG VIỆT, currentValue là giá trị thực tế từ dữ liệu, thresholdValue là ngưỡng cảnh báo để so sánh, recommendedAction BẰNG TIẾNG VIỆT chi tiết 30-50 từ)
12. Phân tích nguyên nhân gốc rễ (chỉ số bất thường: metric BẰNG TIẾNG VIỆT, issue BẰNG TIẾNG VIỆT chi tiết, potentialCauses[] BẰNG TIẾNG VIỆT, mostLikelyCause BẰNG TIẾNG VIỆT chi tiết giải thích TẠI SAO, analysis BẰNG TIẾNG VIỆT 100-150 từ, impactScore 0-100)

BƯỚC 2: JSON DATA (cuối báo cáo, trong thẻ <JSON_DATA>...</JSON_DATA>):

<JSON_DATA>
{{
  ""analysisConclusion"": ""<Kết luận phân tích hệ thống - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch, bắt đầu 'Trong {periodLabel} hiện tại ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd})'. 
  Bao gồm: Tổng lượt đặt (số cụ thể, so kỳ trước %), chuyên khoa nổi bật (tên, số lượt, %), tỉ lệ hủy (số, %, so kỳ trước), lượt hoàn thành (số, %). 
  Phân tích nguyên nhân tăng/giảm (yếu tố chính), đánh giá top bác sĩ/bệnh viện/chuyên khoa (nếu có), xu hướng tổng thể (phát triển/suy giảm), điểm mạnh/yếu chính, tình trạng hệ thống (tốt/trung bình/cần cải thiện).
  Có số liệu cụ thể, phân tích ngắn gọn, KHÔNG có từ tiếng Anh>"",
  
  ""predictionConclusion"": ""<Kết luận dự đoán tương lai - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch về dự đoán. 
  Bao gồm: Dự đoán lượt đặt tuần/tháng tới (số cụ thể, tăng/giảm %, lý do), tỉ lệ hủy dự kiến (%, xu hướng), chuyên khoa phổ biến (tên, lý do), xu hướng khách hàng/bác sĩ/bệnh viện. 
  Đánh giá mức độ tin cậy (cao/trung bình/thấp, lý do), yếu tố ảnh hưởng chính (mùa vụ, sự kiện, chính sách), rủi ro/cơ hội chính, khuyến nghị ngắn gọn.
  Có số liệu cụ thể, lý do rõ ràng, KHÔNG có từ tiếng Anh>"",
  ""predictions"": [
    {{
      ""period"": ""next_week"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }},
    {{
      ""period"": ""next_month"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }}
  ],
  ""alerts"": [
    {{
      ""type"": ""warning|critical|info|success"",
      ""title"": ""<tiêu đề BẰNG TIẾNG VIỆT, ngắn gọn, ví dụ: 'Tỉ lệ hủy lịch cao'>"",
      ""message"": ""<thông điệp BẰNG TIẾNG VIỆT, mô tả chi tiết vấn đề, ít nhất 30-50 từ, ví dụ: 'Tỉ lệ hủy lịch hiện tại là 25%, cao hơn ngưỡng cảnh báo 20%. Điều này cho thấy cần cải thiện chất lượng dịch vụ và tăng cường giao tiếp với khách hàng'>"",
      ""severity"": ""high|medium|low"",
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""currentValue"": <số thực tế từ dữ liệu, ví dụ: nếu tỉ lệ hủy là 25% thì currentValue = 25>,
      ""thresholdValue"": <ngưỡng cảnh báo để so sánh, ví dụ: nếu ngưỡng là 20% thì thresholdValue = 20>,
      ""recommendedAction"": ""<hành động khuyến nghị BẰNG TIẾNG VIỆT, chi tiết, cụ thể, có thể thực hiện ngay, ví dụ: 'Gửi email nhắc nhở lịch hẹn 24h trước khi khám, cải thiện chất lượng dịch vụ khám bệnh, tăng cường giao tiếp với khách hàng qua hotline và SMS', ít nhất 30-50 từ>""
    }}
  ],
  ""rootCauseAnalyses"": [
    {{
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""issue"": ""<mô tả vấn đề BẰNG TIẾNG VIỆT, chi tiết, ví dụ: 'Tỉ lệ hủy lịch cao ở mức 25%, vượt quá ngưỡng cảnh báo 20%'>"",
      ""potentialCauses"": [""<nguyên nhân 1 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng của khách hàng'>"", ""<nguyên nhân 2 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Thiếu giao tiếp và nhắc nhở với khách hàng trước ngày khám'>""],
      ""mostLikelyCause"": ""<nguyên nhân có khả năng cao nhất BẰNG TIẾNG VIỆT, giải thích chi tiết TẠI SAO đây là nguyên nhân chính, dựa trên dữ liệu nào, ít nhất 50-80 từ. Ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng là nguyên nhân chính vì dữ liệu cho thấy tỉ lệ hủy tăng cao sau khi khách hàng đặt lịch, và có nhiều phản hồi tiêu cực về chất lượng dịch vụ. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, cho thấy vấn đề chất lượng dịch vụ đang trở nên nghiêm trọng'>"",
      ""analysis"": ""<phân tích chi tiết BẰNG TIẾNG VIỆT, ít nhất 150-200 từ, giải thích sâu về nguyên nhân gốc rễ, mức độ ảnh hưởng, bằng chứng từ dữ liệu, tại sao nguyên nhân này quan trọng, tác động đến hệ thống như thế nào. Phân tích phải có cấu trúc rõ ràng với các đoạn văn riêng biệt. Ví dụ: 'Phân tích dữ liệu cho thấy rằng chất lượng dịch vụ khám bệnh là nguyên nhân chính dẫn đến việc hủy lịch. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, với hơn 60% lượt hủy xảy ra trong vòng 24 giờ sau khi đặt lịch. Điều này cho thấy khách hàng đang mất niềm tin vào chất lượng dịch vụ ngay sau khi đặt lịch. Dữ liệu phản hồi từ khách hàng cho thấy có nhiều phàn nàn về thời gian chờ đợi lâu, thái độ phục vụ chưa tốt, và chất lượng khám bệnh không đáp ứng kỳ vọng. Tác động của vấn đề này rất lớn, ảnh hưởng trực tiếp đến doanh thu và uy tín của hệ thống. Để giải quyết, cần cải thiện chất lượng dịch vụ khám bệnh, đào tạo nhân viên, và tăng cường giám sát chất lượng dịch vụ'>"",
      ""impactScore"": <số 0-100>
    }}
  ]
}}
</JSON_DATA>

LƯU Ý JSON:
- JSON thuần, KHÔNG có markdown code block (```json)
- KHÔNG có ký tự đặc biệt ngoài JSON
- KHÔNG có comment hoặc text giải thích
- Chỉ JSON object thuần túy";
    }

    private static (List<FuturePrediction> Predictions, List<Alert> Alerts, List<RootCauseAnalysis> RootCauses, string AnalysisConclusion, string PredictionConclusion) ParseStructuredData(
        string aiResponse,
        AiInsightMetrics metrics,
        ILogger<AiInsightsService> logger)
    {
        var predictions = new List<FuturePrediction>();
        var alerts = new List<Alert>();
        var rootCauses = new List<RootCauseAnalysis>();
        var analysisConclusion = string.Empty;
        var predictionConclusion = string.Empty;

        try
        {
            // Extract JSON using pattern from LabResultAnalysisService
            var jsonRoot = ExtractRootJsonElement(aiResponse, logger);
            if (jsonRoot.HasValue)
            {
                // Parse conclusions from JSON
                if (jsonRoot.Value.TryGetProperty("analysisConclusion", out var analysisConclusionProp))
                {
                    analysisConclusion = analysisConclusionProp.GetString() ?? string.Empty;
                }
                else
                {
                    logger.LogWarning("AI response không chứa analysisConclusion trong JSON. Kết luận phân tích hệ thống sẽ trống.");
                }

                if (jsonRoot.Value.TryGetProperty("predictionConclusion", out var predictionConclusionProp))
                {
                    predictionConclusion = predictionConclusionProp.GetString() ?? string.Empty;
                }
                else
                {
                    logger.LogWarning("AI response không chứa predictionConclusion trong JSON. Kết luận dự đoán tương lai sẽ trống.");
                }

                // Parse other JSON data
                ParseJsonData(jsonRoot.Value, predictions, alerts, rootCauses, logger);
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi parse structured data từ AI response: {Message}", ex.Message);
        }

        return (predictions, alerts, rootCauses, analysisConclusion, predictionConclusion);
    }

    /// <summary>
    /// Extract root JSON element from AI response, similar to LabResultAnalysisService pattern
    /// </summary>
    private static JsonElement? ExtractRootJsonElement(string responseText, ILogger<AiInsightsService> logger)
    {
        try
        {
            // First try to find JSON within <JSON_DATA> tags
            var jsonStartTag = responseText.IndexOf("<JSON_DATA>", StringComparison.OrdinalIgnoreCase);
            var jsonEndTag = responseText.IndexOf("</JSON_DATA>", StringComparison.OrdinalIgnoreCase);

            string jsonText;
            if (jsonStartTag >= 0 && jsonEndTag > jsonStartTag)
            {
                jsonText = responseText.Substring(jsonStartTag + 11, jsonEndTag - jsonStartTag - 11).Trim();
            }
            else
            {
                // Fallback: try to find JSON object directly
                var jsonStart = responseText.IndexOf('{');
                var jsonEnd = responseText.LastIndexOf('}');

                if (jsonStart == -1 || jsonEnd == -1)
                {
                    logger.LogWarning("No JSON found in AI response");
                    return null;
                }

                jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // Clean JSON content - remove markdown code blocks if present
            jsonText = CleanJsonContent(jsonText);

            var jsonDoc = JsonDocument.Parse(jsonText);
            return jsonDoc.RootElement;
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Error parsing JSON from AI response");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting JSON from AI response: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Parse JSON data into structured objects
    /// </summary>
    private static void ParseJsonData(
        JsonElement root,
        List<FuturePrediction> predictions,
        List<Alert> alerts,
        List<RootCauseAnalysis> rootCauses,
        ILogger<AiInsightsService> logger)
    {
        try
        {
            // Parse predictions
            if (root.TryGetProperty("predictions", out var predictionsElement) && predictionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var pred in predictionsElement.EnumerateArray())
                {
                    var predictedAppointments = 0;
                    if (pred.TryGetProperty("predictedAppointments", out var pa))
                    {
                        if (pa.ValueKind == JsonValueKind.Number)
                        {
                            try
                            {
                                predictedAppointments = pa.GetInt32();
                            }
                            catch
                            {
                                // Try as double and convert to int
                                predictedAppointments = (int)pa.GetDouble();
                            }
                        }
                        else if (pa.ValueKind == JsonValueKind.String && int.TryParse(pa.GetString(), out var parsed))
                        {
                            predictedAppointments = parsed;
                        }
                    }

                    double predictedGrowthPercent = 0;
                    if (pred.TryGetProperty("predictedGrowthPercent", out var pgp))
                    {
                        if (pgp.ValueKind == JsonValueKind.Number)
                        {
                            predictedGrowthPercent = pgp.GetDouble();
                        }
                        else if (pgp.ValueKind == JsonValueKind.String && double.TryParse(pgp.GetString(), out var parsed))
                        {
                            predictedGrowthPercent = parsed;
                        }
                    }

                    double predictedCancellationRate = 0;
                    if (pred.TryGetProperty("predictedCancellationRate", out var pcr))
                    {
                        if (pcr.ValueKind == JsonValueKind.Number)
                        {
                            predictedCancellationRate = pcr.GetDouble();
                        }
                        else if (pcr.ValueKind == JsonValueKind.String && double.TryParse(pcr.GetString(), out var parsed))
                        {
                            predictedCancellationRate = parsed;
                        }
                    }

                    double predictedRevenue = 0;
                    if (pred.TryGetProperty("predictedRevenue", out var pr))
                    {
                        if (pr.ValueKind == JsonValueKind.Number)
                        {
                            predictedRevenue = pr.GetDouble();
                        }
                        else if (pr.ValueKind == JsonValueKind.String && double.TryParse(pr.GetString(), out var parsed))
                        {
                            predictedRevenue = parsed;
                        }
                    }

                    predictions.Add(new FuturePrediction
                    {
                        Period = pred.TryGetProperty("period", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                        PredictedAppointments = predictedAppointments,
                        PredictedGrowthPercent = predictedGrowthPercent,
                        PredictedCancellationRate = predictedCancellationRate,
                        PredictedRevenue = predictedRevenue,
                        TopSpecialtyPrediction = pred.TryGetProperty("topSpecialtyPrediction", out var tsp) ? tsp.GetString() : null,
                        Confidence = pred.TryGetProperty("confidence", out var conf) ? conf.GetString() ?? "medium" : "medium",
                        Reasoning = pred.TryGetProperty("reasoning", out var reason) ? reason.GetString() ?? string.Empty : string.Empty
                    });
                }
            }
            else
            {
                logger.LogWarning("AI response không chứa predictions trong JSON. Danh sách predictions sẽ trống.");
            }

            // Parse alerts
            if (root.TryGetProperty("alerts", out var alertsElement) && alertsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var alert in alertsElement.EnumerateArray())
                {
                    double? currentValue = null;
                    if (alert.TryGetProperty("currentValue", out var cv))
                    {
                        if (cv.ValueKind == JsonValueKind.Number)
                        {
                            currentValue = cv.GetDouble();
                        }
                        else if (cv.ValueKind == JsonValueKind.String && double.TryParse(cv.GetString(), out var parsed))
                        {
                            currentValue = parsed;
                        }
                    }

                    double? thresholdValue = null;
                    if (alert.TryGetProperty("thresholdValue", out var tv))
                    {
                        if (tv.ValueKind == JsonValueKind.Number)
                        {
                            thresholdValue = tv.GetDouble();
                        }
                        else if (tv.ValueKind == JsonValueKind.String && double.TryParse(tv.GetString(), out var parsed))
                        {
                            thresholdValue = parsed;
                        }
                    }

                    alerts.Add(new Alert
                    {
                        Type = alert.TryGetProperty("type", out var t) ? t.GetString() ?? "info" : "info",
                        Title = alert.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                        Message = alert.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : string.Empty,
                        Severity = alert.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "medium" : "medium",
                        Metric = alert.TryGetProperty("metric", out var met) ? met.GetString() : null,
                        CurrentValue = currentValue,
                        ThresholdValue = thresholdValue,
                        RecommendedAction = alert.TryGetProperty("recommendedAction", out var ra) ? ra.GetString() : null
                    });
                }
            }
            else
            {
                logger.LogWarning("AI response không chứa alerts trong JSON. Danh sách alerts sẽ trống.");
            }

            // Parse root cause analyses
            if (root.TryGetProperty("rootCauseAnalyses", out var rcaElement) && rcaElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var rca in rcaElement.EnumerateArray())
                {
                    var causes = new List<string>();
                    if (rca.TryGetProperty("potentialCauses", out var pc) && pc.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cause in pc.EnumerateArray())
                        {
                            if (cause.ValueKind == JsonValueKind.String)
                            {
                                causes.Add(cause.GetString() ?? string.Empty);
                            }
                        }
                    }

                    double impactScore = 0;
                    if (rca.TryGetProperty("impactScore", out var isc))
                    {
                        if (isc.ValueKind == JsonValueKind.Number)
                        {
                            impactScore = isc.GetDouble();
                        }
                        else if (isc.ValueKind == JsonValueKind.String && double.TryParse(isc.GetString(), out var parsed))
                        {
                            impactScore = parsed;
                        }
                    }

                    rootCauses.Add(new RootCauseAnalysis
                    {
                        Metric = rca.TryGetProperty("metric", out var m) ? m.GetString() ?? string.Empty : string.Empty,
                        Issue = rca.TryGetProperty("issue", out var issue) ? issue.GetString() ?? string.Empty : string.Empty,
                        PotentialCauses = causes,
                        MostLikelyCause = rca.TryGetProperty("mostLikelyCause", out var mlc) ? mlc.GetString() : null,
                        Analysis = rca.TryGetProperty("analysis", out var anal) ? anal.GetString() ?? string.Empty : string.Empty,
                        ImpactScore = impactScore
                    });
                }
            }
            else
            {
                logger.LogWarning("AI response không chứa rootCauseAnalyses trong JSON. Danh sách root causes sẽ trống.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing JSON data: {Message}", ex.Message);
        }
    }

    private static string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return jsonContent;

        // Remove markdown code blocks if present
        jsonContent = jsonContent.Trim();

        // Remove ```json or ``` at start/end
        if (jsonContent.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            jsonContent = jsonContent.Substring(7).TrimStart();
        }
        else if (jsonContent.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            jsonContent = jsonContent.Substring(3).TrimStart();
        }

        if (jsonContent.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            jsonContent = jsonContent.Substring(0, jsonContent.Length - 3).TrimEnd();
        }

        // Remove any leading/trailing whitespace
        jsonContent = jsonContent.Trim();

        return jsonContent;
    }

    public async Task<AiInsightResponse> GenerateForDoctorAsync(
        Guid doctorId,
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime currentStart, currentEnd, previousStart, previousEnd;
            string periodLabel;

            // Nếu có fromDate và toDate, dùng date range tùy chỉnh
            if (request?.FromDate.HasValue == true && request?.ToDate.HasValue == true)
            {
                currentStart = request.FromDate.Value.Date;
                currentEnd = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                var periodLength = (currentEnd - currentStart).TotalDays;
                previousEnd = currentStart.AddTicks(-1);
                previousStart = previousEnd.AddDays(-periodLength).Date;
                periodLabel = $"Tùy chỉnh ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd})";
            }
            else
            {
                var normalizedPeriod = (request?.Period ?? "week").Trim().ToLowerInvariant();
                var (CurrentStart, CurrentEnd, PreviousStart, PreviousEnd, Label) = ResolvePeriod(normalizedPeriod);
                currentStart = CurrentStart;
                currentEnd = CurrentEnd;
                previousStart = PreviousStart;
                previousEnd = PreviousEnd;
                periodLabel = Label;
            }

            _logger.LogInformation("Collecting metrics for doctor {DoctorId}, period {Period} ({Start} to {End})",
                doctorId, periodLabel, currentStart, currentEnd);

            var metrics = await CollectMetricsForDoctorAsync(
                doctorId,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                cancellationToken);

            _logger.LogInformation("Metrics collected for doctor {DoctorId}. Total appointments: {Total}", doctorId, metrics.CurrentTotal);

            // Lấy tên bác sĩ
            string doctorName = await ResolveDoctorNameAsync(doctorId, cancellationToken);

            var prompt = BuildPromptForDoctor(
                doctorId,
                doctorName,
                metrics,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                periodLabel);

            _logger.LogInformation("Calling Groq API for doctor {DoctorId} with model {Model}", doctorId, _groqConfig.PrimaryModel);

            var summary = await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _groqConfig,
                temperature: _groqConfig.Temperature ?? 0.2,
                maxTokens: _groqConfig.MaxTokens ?? 6000,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Groq API response received for doctor {DoctorId}. Summary length: {Length}", doctorId, summary?.Length ?? 0);

            var (predictions, alerts, rootCauses, analysisConclusion, predictionConclusion) = ParseStructuredData(summary ?? string.Empty, metrics, _logger);

            return new AiInsightResponse
            {
                Summary = summary?.Trim() ?? string.Empty,
                Period = periodLabel,
                PeriodStart = currentStart,
                PeriodEnd = currentEnd,
                GeneratedAt = DateTime.UtcNow,
                Model = _groqConfig.PrimaryModel,
                Metrics = metrics,
                Predictions = predictions,
                Alerts = alerts,
                RootCauseAnalyses = rootCauses,
                AnalysisConclusion = analysisConclusion,
                PredictionConclusion = predictionConclusion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights for doctor {DoctorId}: {Message}", doctorId, ex.Message);
            throw;
        }
    }

    public async Task<AiInsightResponse> GenerateForHospitalAsync(
        Guid hospitalId,
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime currentStart, currentEnd, previousStart, previousEnd;
            string periodLabel;

            // Nếu có fromDate và toDate, dùng date range tùy chỉnh
            if (request?.FromDate.HasValue == true && request?.ToDate.HasValue == true)
            {
                currentStart = request.FromDate.Value.Date;
                currentEnd = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                var periodLength = (currentEnd - currentStart).TotalDays;
                previousEnd = currentStart.AddTicks(-1);
                previousStart = previousEnd.AddDays(-periodLength).Date;
                periodLabel = $"Tùy chỉnh ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd})";
            }
            else
            {
                var normalizedPeriod = (request?.Period ?? "week").Trim().ToLowerInvariant();
                var (CurrentStart, CurrentEnd, PreviousStart, PreviousEnd, Label) = ResolvePeriod(normalizedPeriod);
                currentStart = CurrentStart;
                currentEnd = CurrentEnd;
                previousStart = PreviousStart;
                previousEnd = PreviousEnd;
                periodLabel = Label;
            }

            _logger.LogInformation("Collecting metrics for hospital {HospitalId}, period {Period} ({Start} to {End})",
                hospitalId, periodLabel, currentStart, currentEnd);

            var metrics = await CollectMetricsForHospitalAsync(
                hospitalId,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                cancellationToken);

            _logger.LogInformation("Metrics collected for hospital {HospitalId}. Total appointments: {Total}", hospitalId, metrics.CurrentTotal);

            // Lấy tên bệnh viện
            string hospitalName = await ResolveHospitalNameAsync(hospitalId, cancellationToken);

            var prompt = BuildPromptForHospital(
                hospitalId,
                hospitalName,
                metrics,
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                periodLabel);

            _logger.LogInformation("Calling Groq API for hospital {HospitalId} with model {Model}", hospitalId, _groqConfig.PrimaryModel);

            var summary = await _groqApiHelper.CallGroqApiAsync(
                prompt,
                _groqConfig,
                temperature: _groqConfig.Temperature ?? 0.2,
                maxTokens: _groqConfig.MaxTokens ?? 6000,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Groq API response received for hospital {HospitalId}. Summary length: {Length}", hospitalId, summary?.Length ?? 0);

            var (predictions, alerts, rootCauses, analysisConclusion, predictionConclusion) = ParseStructuredData(summary ?? string.Empty, metrics, _logger);

            return new AiInsightResponse
            {
                Summary = summary?.Trim() ?? string.Empty,
                Period = periodLabel,
                PeriodStart = currentStart,
                PeriodEnd = currentEnd,
                GeneratedAt = DateTime.UtcNow,
                Model = _groqConfig.PrimaryModel,
                Metrics = metrics,
                Predictions = predictions,
                Alerts = alerts,
                RootCauseAnalyses = rootCauses,
                AnalysisConclusion = analysisConclusion,
                PredictionConclusion = predictionConclusion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights for hospital {HospitalId}: {Message}", hospitalId, ex.Message);
            throw;
        }
    }

    private async Task<AiInsightMetrics> CollectMetricsForDoctorAsync(
        Guid doctorId,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(_appointmentConnection);
            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Collecting basic metrics for doctor {DoctorId}...", doctorId);
            var currentTotal = await CountAppointmentsAsync(connection, currentStart, currentEnd, null, cancellationToken, doctorId: doctorId);
            var previousTotal = await CountAppointmentsAsync(connection, previousStart, previousEnd, null, cancellationToken, doctorId: doctorId);
            var currentCancelled = await CountAppointmentsAsync(connection, currentStart, currentEnd, "CANCELLED", cancellationToken, doctorId: doctorId);
            var previousCancelled = await CountAppointmentsAsync(connection, previousStart, previousEnd, "CANCELLED", cancellationToken, doctorId: doctorId);
            var currentCompleted = await CountAppointmentsAsync(connection, currentStart, currentEnd, "COMPLETED", cancellationToken, doctorId: doctorId);

            _logger.LogInformation("Getting top specialty for doctor {DoctorId}...", doctorId);
            var topSpecialty = await GetTopSpecialtyAsync(connection, currentStart, currentEnd, cancellationToken, doctorId: doctorId);
            var topSpecialtyName = await ResolveSpecialtyNameAsync(topSpecialty?.SpecialtyId, cancellationToken);

            // Extended analytics
            _logger.LogInformation("Collecting extended analytics for doctor {DoctorId}...", doctorId);
            var peakHours = await GetPeakHoursAsync(connection, currentStart, currentEnd, cancellationToken, doctorId: doctorId);
            var specialtyBreakdown = await GetSpecialtyBreakdownAsync(connection, currentStart, currentEnd, previousStart, previousEnd, cancellationToken, doctorId: doctorId);
            var cancellationDetails = await GetCancellationDetailsAsync(connection, currentStart, currentEnd, cancellationToken, doctorId: doctorId);
            var patientSegments = await GetPatientSegmentsAsync(connection, currentStart, currentEnd, cancellationToken, doctorId: doctorId);

            return new AiInsightMetrics
            {
                CurrentTotal = currentTotal,
                PreviousTotal = previousTotal,
                GrowthPercent = CalculateDeltaPercent(currentTotal, previousTotal),
                CurrentCompleted = currentCompleted,
                CurrentCancelled = currentCancelled,
                CancellationRate = currentTotal == 0 ? 0 : Math.Round((double)currentCancelled / currentTotal * 100, 2),
                CancellationDeltaPercent = CalculateDeltaPercent(currentCancelled, previousCancelled),
                TopSpecialtyId = topSpecialty?.SpecialtyId,
                TopSpecialtyName = topSpecialtyName,
                TopSpecialtyCount = topSpecialty?.Count ?? 0,
                TopSpecialtyShare = currentTotal == 0
                    ? 0
                    : Math.Round((double)(topSpecialty?.Count ?? 0) / currentTotal * 100, 2),
                TopDoctors = new List<DoctorUtilization>(), // Not applicable for doctor view
                TopHospitals = new List<HospitalStats>(), // Not applicable for doctor view
                PeakHours = peakHours,
                SpecialtyBreakdown = specialtyBreakdown,
                CancellationDetails = cancellationDetails,
                PatientSegments = patientSegments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics for doctor {DoctorId}: {Message}", doctorId, ex.Message);
            throw;
        }
    }

    private async Task<AiInsightMetrics> CollectMetricsForHospitalAsync(
        Guid hospitalId,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(_appointmentConnection);
            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Collecting basic metrics for hospital {HospitalId}...", hospitalId);
            var currentTotal = await CountAppointmentsAsync(connection, currentStart, currentEnd, null, cancellationToken, hospitalId: hospitalId);
            var previousTotal = await CountAppointmentsAsync(connection, previousStart, previousEnd, null, cancellationToken, hospitalId: hospitalId);
            var currentCancelled = await CountAppointmentsAsync(connection, currentStart, currentEnd, "CANCELLED", cancellationToken, hospitalId: hospitalId);
            var previousCancelled = await CountAppointmentsAsync(connection, previousStart, previousEnd, "CANCELLED", cancellationToken, hospitalId: hospitalId);
            var currentCompleted = await CountAppointmentsAsync(connection, currentStart, currentEnd, "COMPLETED", cancellationToken, hospitalId: hospitalId);

            _logger.LogInformation("Getting top specialty for hospital {HospitalId}...", hospitalId);
            var topSpecialty = await GetTopSpecialtyAsync(connection, currentStart, currentEnd, cancellationToken, hospitalId: hospitalId);
            var topSpecialtyName = await ResolveSpecialtyNameAsync(topSpecialty?.SpecialtyId, cancellationToken);

            // Extended analytics
            _logger.LogInformation("Collecting extended analytics for hospital {HospitalId}...", hospitalId);
            var topDoctors = await GetTopDoctorsAsync(connection, currentStart, currentEnd, cancellationToken, hospitalId: hospitalId);
            var peakHours = await GetPeakHoursAsync(connection, currentStart, currentEnd, cancellationToken, hospitalId: hospitalId);
            var specialtyBreakdown = await GetSpecialtyBreakdownAsync(connection, currentStart, currentEnd, previousStart, previousEnd, cancellationToken, hospitalId: hospitalId);
            var cancellationDetails = await GetCancellationDetailsAsync(connection, currentStart, currentEnd, cancellationToken, hospitalId: hospitalId);
            var patientSegments = await GetPatientSegmentsAsync(connection, currentStart, currentEnd, cancellationToken, hospitalId: hospitalId);

            return new AiInsightMetrics
            {
                CurrentTotal = currentTotal,
                PreviousTotal = previousTotal,
                GrowthPercent = CalculateDeltaPercent(currentTotal, previousTotal),
                CurrentCompleted = currentCompleted,
                CurrentCancelled = currentCancelled,
                CancellationRate = currentTotal == 0 ? 0 : Math.Round((double)currentCancelled / currentTotal * 100, 2),
                CancellationDeltaPercent = CalculateDeltaPercent(currentCancelled, previousCancelled),
                TopSpecialtyId = topSpecialty?.SpecialtyId,
                TopSpecialtyName = topSpecialtyName,
                TopSpecialtyCount = topSpecialty?.Count ?? 0,
                TopSpecialtyShare = currentTotal == 0
                    ? 0
                    : Math.Round((double)(topSpecialty?.Count ?? 0) / currentTotal * 100, 2),
                TopDoctors = topDoctors,
                TopHospitals = new List<HospitalStats>(), // Not applicable for hospital view
                PeakHours = peakHours,
                SpecialtyBreakdown = specialtyBreakdown,
                CancellationDetails = cancellationDetails,
                PatientSegments = patientSegments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics for hospital {HospitalId}: {Message}", hospitalId, ex.Message);
            throw;
        }
    }

    private static string BuildPromptForDoctor(
        Guid doctorId,
        string doctorName,
        AiInsightMetrics metrics,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        string periodLabel)
    {
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });

        return $@"
Bạn là chuyên gia phân tích dữ liệu BI cho BookingCare - hệ thống đặt lịch khám bệnh trực tuyến.
Phân tích dữ liệu và tạo báo cáo insights chuyên nghiệp bằng TIẾNG VIỆT cho bác sĩ {doctorName}.

⚠️ YÊU CẦU BẮT BUỘC:
- TẤT CẢ nội dung phải bằng TIẾNG VIỆT HOÀN TOÀN (kết luận, phân tích, dự đoán, tên chuyên khoa, khuyến nghị, cảnh báo)
- TUYỆT ĐỐI KHÔNG được dùng tiếng Trung, tiếng Anh, hoặc bất kỳ ngôn ngữ nào khác (trừ tên riêng)
- Sử dụng số liệu cụ thể từ dữ liệu, KHÔNG tạo dữ liệu giả
- Viết thành đoạn văn liền mạch, không phải bullet points
- Độ dài báo cáo: 1500-2800 từ
- Tập trung vào hiệu suất CÁ NHÂN của bác sĩ: lịch hẹn, khung giờ, bệnh nhân mới/quay lại, chất lượng dịch vụ

BỐI CẢNH:
- Bác sĩ: {doctorName}
- Giai đoạn hiện tại: {currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd} ({periodLabel})
- Giai đoạn so sánh: {previousStart:yyyy-MM-dd} → {previousEnd:yyyy-MM-dd}

DỮ LIỆU THỐNG KÊ:
{metricsJson}

CẤU TRÚC BÁO CÁO:

BƯỚC 1: PHÂN TÍCH VĂN BẢN (viết trước JSON):
Phân tích chi tiết các khía cạnh sau (mỗi phần 100-180 từ), ưu tiên góc nhìn CÁ NHÂN của bác sĩ:
1. Tổng quan hiệu suất cá nhân (lượt đặt, hoàn thành, hủy, % tăng/giảm vs kỳ trước)
2. Chuyên khoa bác sĩ đang phục vụ (lượt, % share, tăng/giảm, so sánh với trung bình)
3. Khung giờ vàng của bác sĩ (giờ nhiều lượt nhất, đề xuất mở/điều chỉnh ca làm việc)
4. Bệnh nhân mới vs quay lại, xu hướng retention với bác sĩ (tỷ lệ bệnh nhân quay lại khám)
5. Hủy lịch gắn với bác sĩ (tỷ lệ, lý do, pattern theo giờ/ngày trong tuần, tác động đến uy tín)
6. Đề xuất cải thiện cụ thể cho bác sĩ (5-7 ý: khung giờ, follow-up, nhắc hẹn, chất lượng dịch vụ, giao tiếp với bệnh nhân)
7. Rủi ro cá nhân (3-5: mô tả, mức độ, khả năng, tác động, biện pháp khắc phục)
8. Dự đoán cá nhân (lượt tuần/tháng tới, % hủy dự kiến, confidence, lý do, giờ nên mở thêm)
9. Cảnh báo cá nhân (type, title, message, severity, metric, currentValue, thresholdValue, recommendedAction)
10. Phân tích nguyên nhân gốc rễ cho các chỉ số bất thường của bác sĩ (metric, issue, potentialCauses[], mostLikelyCause, analysis 100-150 từ, impactScore)

BƯỚC 2: JSON DATA (cuối báo cáo, trong thẻ <JSON_DATA>...</JSON_DATA>):
Sử dụng cùng cấu trúc JSON như báo cáo bệnh viện nhưng CHỈ tập trung vào dữ liệu của bác sĩ này (điền metric theo dữ liệu đã cho).

<JSON_DATA>
{{
  ""analysisConclusion"": ""<Kết luận phân tích - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch, bắt đầu 'Trong {periodLabel} hiện tại ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd}), bác sĩ {doctorName}'. 
  Bao gồm: Tổng lượt đặt (số cụ thể, so kỳ trước %), chuyên khoa đang phục vụ (tên, số lượt, %), tỉ lệ hủy (số, %, so kỳ trước), lượt hoàn thành (số, %). 
  Phân tích nguyên nhân tăng/giảm (yếu tố chính), đánh giá hiệu suất cá nhân (tốt/trung bình/cần cải thiện), xu hướng bệnh nhân (mới/quay lại), điểm mạnh/yếu chính.
  Có số liệu cụ thể, phân tích ngắn gọn, KHÔNG có từ tiếng Anh>"",
  
  ""predictionConclusion"": ""<Kết luận dự đoán tương lai - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch về dự đoán cho bác sĩ. 
  Bao gồm: Dự đoán lượt đặt tuần/tháng tới (số cụ thể, tăng/giảm %, lý do), tỉ lệ hủy dự kiến (%, xu hướng), chuyên khoa tiếp tục phục vụ (tên, lý do), xu hướng bệnh nhân. 
  Đánh giá mức độ tin cậy (cao/trung bình/thấp, lý do), yếu tố ảnh hưởng chính (mùa vụ, sự kiện, lịch làm việc), rủi ro/cơ hội chính, khuyến nghị ngắn gọn.
  Có số liệu cụ thể, lý do rõ ràng, KHÔNG có từ tiếng Anh>"",
  ""predictions"": [
    {{
      ""period"": ""next_week"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }},
    {{
      ""period"": ""next_month"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }}
  ],
  ""alerts"": [
    {{
      ""type"": ""warning|critical|info|success"",
      ""title"": ""<tiêu đề BẰNG TIẾNG VIỆT, ngắn gọn, ví dụ: 'Tỉ lệ hủy lịch cao'>"",
      ""message"": ""<thông điệp BẰNG TIẾNG VIỆT, mô tả chi tiết vấn đề, ít nhất 30-50 từ, ví dụ: 'Tỉ lệ hủy lịch hiện tại là 25%, cao hơn ngưỡng cảnh báo 20%. Điều này cho thấy cần cải thiện chất lượng dịch vụ và tăng cường giao tiếp với bệnh nhân'>"",
      ""severity"": ""high|medium|low"",
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""currentValue"": <số thực tế từ dữ liệu, ví dụ: nếu tỉ lệ hủy là 25% thì currentValue = 25>,
      ""thresholdValue"": <ngưỡng cảnh báo để so sánh, ví dụ: nếu ngưỡng là 20% thì thresholdValue = 20>,
      ""recommendedAction"": ""<hành động khuyến nghị BẰNG TIẾNG VIỆT, chi tiết, cụ thể, có thể thực hiện ngay, ví dụ: 'Gửi tin nhắn nhắc nhở bệnh nhân 24h trước khi khám, cải thiện chất lượng khám bệnh, tăng cường giao tiếp với bệnh nhân qua điện thoại', ít nhất 30-50 từ>""
    }}
  ],
  ""rootCauseAnalyses"": [
    {{
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""issue"": ""<mô tả vấn đề BẰNG TIẾNG VIỆT, chi tiết, ví dụ: 'Tỉ lệ hủy lịch cao ở mức 25%, vượt quá ngưỡng cảnh báo 20%'>"",
      ""potentialCauses"": [""<nguyên nhân 1 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng của bệnh nhân'>"", ""<nguyên nhân 2 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Thiếu giao tiếp và nhắc nhở với bệnh nhân trước ngày khám'>""],
      ""mostLikelyCause"": ""<nguyên nhân có khả năng cao nhất BẰNG TIẾNG VIỆT, giải thích chi tiết TẠI SAO đây là nguyên nhân chính, dựa trên dữ liệu nào, ít nhất 50-80 từ. Ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng là nguyên nhân chính vì dữ liệu cho thấy tỉ lệ hủy tăng cao sau khi bệnh nhân đặt lịch, và có nhiều phản hồi tiêu cực về chất lượng dịch vụ. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, cho thấy vấn đề chất lượng dịch vụ đang trở nên nghiêm trọng'>"",
      ""analysis"": ""<phân tích chi tiết BẰNG TIẾNG VIỆT, ít nhất 150-200 từ, giải thích sâu về nguyên nhân gốc rễ, mức độ ảnh hưởng, bằng chứng từ dữ liệu, tại sao nguyên nhân này quan trọng, tác động đến uy tín bác sĩ như thế nào. Phân tích phải có cấu trúc rõ ràng với các đoạn văn riêng biệt. Ví dụ: 'Phân tích dữ liệu cho thấy rằng chất lượng dịch vụ khám bệnh là nguyên nhân chính dẫn đến việc hủy lịch. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, với hơn 60% lượt hủy xảy ra trong vòng 24 giờ sau khi đặt lịch. Điều này cho thấy bệnh nhân đang mất niềm tin vào chất lượng dịch vụ ngay sau khi đặt lịch. Dữ liệu phản hồi từ bệnh nhân cho thấy có nhiều phàn nàn về thời gian chờ đợi lâu, thái độ phục vụ chưa tốt, và chất lượng khám bệnh không đáp ứng kỳ vọng. Tác động của vấn đề này rất lớn, ảnh hưởng trực tiếp đến uy tín và thu nhập của bác sĩ. Để giải quyết, cần cải thiện chất lượng dịch vụ khám bệnh, tăng cường giao tiếp với bệnh nhân, và chú ý đến thời gian khám'>"",
      ""impactScore"": <số 0-100>
    }}
  ]
}}
</JSON_DATA>

LƯU Ý JSON:
- JSON thuần, KHÔNG có markdown code block (```json)
- KHÔNG có ký tự đặc biệt ngoài JSON
- KHÔNG có comment hoặc text giải thích
- Chỉ JSON object thuần túy";
    }

    private static string BuildPromptForHospital(
        Guid hospitalId,
        string hospitalName,
        AiInsightMetrics metrics,
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd,
        string periodLabel)
    {
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });

        return $@"
Bạn là chuyên gia phân tích dữ liệu BI cho BookingCare - hệ thống đặt lịch khám bệnh trực tuyến.
Phân tích dữ liệu và tạo báo cáo insights chuyên nghiệp bằng TIẾNG VIỆT cho bệnh viện {hospitalName}.

⚠️ YÊU CẦU BẮT BUỘC:
- TẤT CẢ nội dung phải bằng TIẾNG VIỆT HOÀN TOÀN (kết luận, phân tích, dự đoán, tên chuyên khoa/bác sĩ, khuyến nghị, cảnh báo)
- TUYỆT ĐỐI KHÔNG được dùng tiếng Trung, tiếng Anh, hoặc bất kỳ ngôn ngữ nào khác (trừ tên riêng)
- Sử dụng số liệu cụ thể từ dữ liệu, KHÔNG tạo dữ liệu giả
- Viết thành đoạn văn liền mạch, không phải bullet points
- Độ dài báo cáo: 1500-2800 từ
- Tập trung vào hiệu suất VẬN HÀNH bệnh viện: phân bổ bác sĩ, chuyên khoa, khung giờ, bệnh nhân mới/quay lại, chất lượng dịch vụ

BỐI CẢNH:
- Bệnh viện: {hospitalName}
- Giai đoạn hiện tại: {currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd} ({periodLabel})
- Giai đoạn so sánh: {previousStart:yyyy-MM-dd} → {previousEnd:yyyy-MM-dd}

DỮ LIỆU THỐNG KÊ:
{metricsJson}

CẤU TRÚC BÁO CÁO:

BƯỚC 1: PHÂN TÍCH VĂN BẢN (viết trước JSON):
Phân tích chi tiết các khía cạnh sau (mỗi phần 100-180 từ), ưu tiên góc nhìn VẬN HÀNH bệnh viện:
1. Tổng quan hiệu suất bệnh viện (lượt đặt, hoàn thành, hủy, % tăng/giảm vs kỳ trước)
2. Top bác sĩ & phân bổ việc (số lượt, % share, quá tải/thiếu việc dựa trên dữ liệu)
3. Chuyên khoa: top, % share, tăng trưởng; chuyên khoa tụt giảm cần hỗ trợ
4. Khung giờ vàng toàn bệnh viện (giờ nhiều lượt nhất, đề xuất điều phối/giãn tải)
5. Bệnh nhân: mới vs quay lại, retention của bệnh viện
6. Hủy lịch theo bệnh viện (tỷ lệ, lý do, pattern theo bác sĩ/chuyên khoa/giờ, tác động)
7. Đề xuất hành động cho bệnh viện (5-7 ý: điều phối bác sĩ, mở/đóng slot, hỗ trợ chuyên khoa yếu, nhắc hẹn)
8. Rủi ro vận hành (3-5: mô tả, mức độ, khả năng, tác động, biện pháp)
9. Dự đoán cho bệnh viện (lượt tuần/tháng tới, % hủy dự kiến, chuyên khoa có thể dẫn dắt, confidence, lý do)
10. Cảnh báo vận hành (type, title, message, severity, metric, currentValue, thresholdValue, recommendedAction)
11. Phân tích nguyên nhân gốc rễ cho các chỉ số bất thường của bệnh viện (metric, issue, potentialCauses[], mostLikelyCause, analysis 100-150 từ, impactScore)

BƯỚC 2: JSON DATA (cuối báo cáo, trong thẻ <JSON_DATA>...</JSON_DATA>):
Sử dụng cùng cấu trúc JSON như báo cáo admin nhưng CHỈ tập trung vào dữ liệu của bệnh viện này (điền metric theo dữ liệu đã cho).

<JSON_DATA>
{{
  ""analysisConclusion"": ""<Kết luận phân tích hệ thống - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch, bắt đầu 'Trong {periodLabel} hiện tại ({currentStart:yyyy-MM-dd} → {currentEnd:yyyy-MM-dd})'. 
  Bao gồm: Tổng lượt đặt (số cụ thể, so kỳ trước %), chuyên khoa nổi bật (tên, số lượt, %), tỉ lệ hủy (số, %, so kỳ trước), lượt hoàn thành (số, %). 
  Phân tích nguyên nhân tăng/giảm (yếu tố chính), đánh giá top bác sĩ/bệnh viện/chuyên khoa (nếu có), xu hướng tổng thể (phát triển/suy giảm), điểm mạnh/yếu chính, tình trạng hệ thống (tốt/trung bình/cần cải thiện).
  Có số liệu cụ thể, phân tích ngắn gọn, KHÔNG có từ tiếng Anh>"",
  
  ""predictionConclusion"": ""<Kết luận dự đoán tương lai - 250-300 TỪ, BẰNG TIẾNG VIỆT HOÀN TOÀN.
  
  Viết một đoạn văn liền mạch về dự đoán. 
  Bao gồm: Dự đoán lượt đặt tuần/tháng tới (số cụ thể, tăng/giảm %, lý do), tỉ lệ hủy dự kiến (%, xu hướng), chuyên khoa phổ biến (tên, lý do), xu hướng khách hàng/bác sĩ/bệnh viện. 
  Đánh giá mức độ tin cậy (cao/trung bình/thấp, lý do), yếu tố ảnh hưởng chính (mùa vụ, sự kiện, chính sách), rủi ro/cơ hội chính, khuyến nghị ngắn gọn.
  Có số liệu cụ thể, lý do rõ ràng, KHÔNG có từ tiếng Anh>"",
  ""predictions"": [
    {{
      ""period"": ""next_week"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }},
    {{
      ""period"": ""next_month"",
      ""predictedAppointments"": <số>,
      ""predictedGrowthPercent"": <số>,
      ""predictedCancellationRate"": <số>,
      ""predictedRevenue"": <số>,
      ""topSpecialtyPrediction"": ""<tên chuyên khoa>"",
      ""confidence"": ""high|medium|low"",
      ""reasoning"": ""<lý do>""
    }}
  ],
  ""alerts"": [
    {{
      ""type"": ""warning|critical|info|success"",
      ""title"": ""<tiêu đề BẰNG TIẾNG VIỆT, ngắn gọn, ví dụ: 'Tỉ lệ hủy lịch cao'>"",
      ""message"": ""<thông điệp BẰNG TIẾNG VIỆT, mô tả chi tiết vấn đề, ít nhất 30-50 từ, ví dụ: 'Tỉ lệ hủy lịch hiện tại là 25%, cao hơn ngưỡng cảnh báo 20%. Điều này cho thấy cần cải thiện chất lượng dịch vụ và tăng cường giao tiếp với khách hàng'>"",
      ""severity"": ""high|medium|low"",
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""currentValue"": <số thực tế từ dữ liệu, ví dụ: nếu tỉ lệ hủy là 25% thì currentValue = 25>,
      ""thresholdValue"": <ngưỡng cảnh báo để so sánh, ví dụ: nếu ngưỡng là 20% thì thresholdValue = 20>,
      ""recommendedAction"": ""<hành động khuyến nghị BẰNG TIẾNG VIỆT, chi tiết, cụ thể, có thể thực hiện ngay, ví dụ: 'Gửi email nhắc nhở lịch hẹn 24h trước khi khám, cải thiện chất lượng dịch vụ khám bệnh, tăng cường giao tiếp với khách hàng qua hotline và SMS', ít nhất 30-50 từ>""
    }}
  ],
  ""rootCauseAnalyses"": [
    {{
      ""metric"": ""<tên chỉ số BẰNG TIẾNG VIỆT, ví dụ: 'Tỉ lệ hủy lịch'>"",
      ""issue"": ""<mô tả vấn đề BẰNG TIẾNG VIỆT, chi tiết, ví dụ: 'Tỉ lệ hủy lịch cao ở mức 25%, vượt quá ngưỡng cảnh báo 20%'>"",
      ""potentialCauses"": [""<nguyên nhân 1 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng của khách hàng'>"", ""<nguyên nhân 2 BẰNG TIẾNG VIỆT, mô tả cụ thể, ví dụ: 'Thiếu giao tiếp và nhắc nhở với khách hàng trước ngày khám'>""],
      ""mostLikelyCause"": ""<nguyên nhân có khả năng cao nhất BẰNG TIẾNG VIỆT, giải thích chi tiết TẠI SAO đây là nguyên nhân chính, dựa trên dữ liệu nào, ít nhất 50-80 từ. Ví dụ: 'Chất lượng dịch vụ khám bệnh chưa đáp ứng kỳ vọng là nguyên nhân chính vì dữ liệu cho thấy tỉ lệ hủy tăng cao sau khi khách hàng đặt lịch, và có nhiều phản hồi tiêu cực về chất lượng dịch vụ. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, cho thấy vấn đề chất lượng dịch vụ đang trở nên nghiêm trọng'>"",
      ""analysis"": ""<phân tích chi tiết BẰNG TIẾNG VIỆT, ít nhất 150-200 từ, giải thích sâu về nguyên nhân gốc rễ, mức độ ảnh hưởng, bằng chứng từ dữ liệu, tại sao nguyên nhân này quan trọng, tác động đến hệ thống như thế nào. Phân tích phải có cấu trúc rõ ràng với các đoạn văn riêng biệt. Ví dụ: 'Phân tích dữ liệu cho thấy rằng chất lượng dịch vụ khám bệnh là nguyên nhân chính dẫn đến việc hủy lịch. Tỉ lệ hủy tăng từ 15% lên 25% trong 2 tuần qua, với hơn 60% lượt hủy xảy ra trong vòng 24 giờ sau khi đặt lịch. Điều này cho thấy khách hàng đang mất niềm tin vào chất lượng dịch vụ ngay sau khi đặt lịch. Dữ liệu phản hồi từ khách hàng cho thấy có nhiều phàn nàn về thời gian chờ đợi lâu, thái độ phục vụ chưa tốt, và chất lượng khám bệnh không đáp ứng kỳ vọng. Tác động của vấn đề này rất lớn, ảnh hưởng trực tiếp đến doanh thu và uy tín của hệ thống. Để giải quyết, cần cải thiện chất lượng dịch vụ khám bệnh, đào tạo nhân viên, và tăng cường giám sát chất lượng dịch vụ'>"",
      ""impactScore"": <số 0-100>
    }}
  ]
}}
</JSON_DATA>

LƯU Ý JSON:
- JSON thuần, KHÔNG có markdown code block (```json)
- KHÔNG có ký tự đặc biệt ngoài JSON
- KHÔNG có comment hoặc text giải thích
- Chỉ JSON object thuần túy";
    }

}


