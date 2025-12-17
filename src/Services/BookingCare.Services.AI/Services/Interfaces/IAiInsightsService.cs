using BookingCare.Services.AI.Models.DTOs.Insights;

namespace BookingCare.Services.AI.Services.Interfaces;

public interface IAiInsightsService
{
    Task<AiInsightResponse> GenerateAsync(
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default
    );

    Task<AiInsightResponse> GenerateForDoctorAsync(
        Guid doctorId,
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default
    );

    Task<AiInsightResponse> GenerateForHospitalAsync(
        Guid hospitalId,
        GenerateAiInsightRequest request,
        CancellationToken cancellationToken = default
    );
}



