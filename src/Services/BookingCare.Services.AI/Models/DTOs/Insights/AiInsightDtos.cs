using System;
using System.Collections.Generic;

namespace BookingCare.Services.AI.Models.DTOs.Insights;

public class GenerateAiInsightRequest
{
    /// <summary>
    /// Period để tổng hợp (ví dụ: week, month). Mặc định: week.
    /// Chỉ dùng khi không có fromDate/toDate.
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Ngày bắt đầu (tùy chỉnh). Nếu có, sẽ ưu tiên dùng thay vì Period.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (tùy chỉnh). Nếu có, sẽ ưu tiên dùng thay vì Period.
    /// </summary>
    public DateTime? ToDate { get; set; }
}

public class DoctorUtilization
{
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public int AppointmentCount { get; set; }
    public double UtilizationPercent { get; set; }
}

public class HospitalStats
{
    public Guid HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public int AppointmentCount { get; set; }
    public double SharePercent { get; set; }
}

public class PeakHourStats
{
    public int TimeSlotId { get; set; }
    public string? TimeSlotLabel { get; set; }
    public int AppointmentCount { get; set; }
    public double SharePercent { get; set; }
}

public class SpecialtyStats
{
    public Guid SpecialtyId { get; set; }
    public string? SpecialtyName { get; set; }
    public int AppointmentCount { get; set; }
    public double SharePercent { get; set; }
    public double GrowthPercent { get; set; }
}

public class CancellationInsight
{
    public int TotalCancelled { get; set; }
    public int CancelledByPatient { get; set; }
    public int CancelledBySystem { get; set; }
    public double CancellationRate { get; set; }
    public Dictionary<string, int> CancellationReasons { get; set; } = new();
}

public class PatientSegmentStats
{
    public int NewPatients { get; set; }
    public int ReturningPatients { get; set; }
    public double NewPatientRate { get; set; }
    public double ReturningPatientRate { get; set; }
}

public class FuturePrediction
{
    public string Period { get; set; } = string.Empty; // "next_week", "next_month", "next_quarter"
    public int PredictedAppointments { get; set; }
    public double PredictedGrowthPercent { get; set; }
    public double PredictedCancellationRate { get; set; }
    public double PredictedRevenue { get; set; }
    public string? TopSpecialtyPrediction { get; set; }
    public string Confidence { get; set; } = "medium"; // "high", "medium", "low"
    public string Reasoning { get; set; } = string.Empty;
}

public class Alert
{
    public string Type { get; set; } = string.Empty; // "warning", "critical", "info", "success"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium"; // "high", "medium", "low"
    public string? Metric { get; set; } // Which metric triggered this alert
    public double? CurrentValue { get; set; }
    public double? ThresholdValue { get; set; }
    public string? RecommendedAction { get; set; }
}

public class RootCauseAnalysis
{
    public string Metric { get; set; } = string.Empty; // Which metric is being analyzed
    public string Issue { get; set; } = string.Empty; // Description of the issue
    public List<string> PotentialCauses { get; set; } = new();
    public string? MostLikelyCause { get; set; }
    public string Analysis { get; set; } = string.Empty;
    public double ImpactScore { get; set; } // 0-100, how significant is this issue
}

public class AiInsightMetrics
{
    // Basic metrics
    public int CurrentTotal { get; set; }
    public int PreviousTotal { get; set; }
    public double GrowthPercent { get; set; }
    public int CurrentCompleted { get; set; }
    public int CurrentCancelled { get; set; }
    public double CancellationRate { get; set; }
    public double CancellationDeltaPercent { get; set; }
    
    // Specialty metrics
    public Guid? TopSpecialtyId { get; set; }
    public string? TopSpecialtyName { get; set; }
    public int TopSpecialtyCount { get; set; }
    public double TopSpecialtyShare { get; set; }
    
    // Extended analytics
    public List<DoctorUtilization> TopDoctors { get; set; } = new();
    public List<HospitalStats> TopHospitals { get; set; } = new();
    public List<PeakHourStats> PeakHours { get; set; } = new();
    public List<SpecialtyStats> SpecialtyBreakdown { get; set; } = new();
    public CancellationInsight CancellationDetails { get; set; } = new();
    public PatientSegmentStats PatientSegments { get; set; } = new();
}

public class AiInsightResponse
{
    public string Summary { get; set; } = string.Empty;
    public string Period { get; set; } = "week";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Model { get; set; } = string.Empty;
    public AiInsightMetrics Metrics { get; set; } = new();
    
    // New fields for enhanced insights
    public List<FuturePrediction> Predictions { get; set; } = new();
    public List<Alert> Alerts { get; set; } = new();
    public List<RootCauseAnalysis> RootCauseAnalyses { get; set; } = new();
    
    // Conclusion texts
    public string AnalysisConclusion { get; set; } = string.Empty; // Kết luận phân tích dạng văn bản
    public string PredictionConclusion { get; set; } = string.Empty; // Kết luận dự đoán dạng văn bản
}


