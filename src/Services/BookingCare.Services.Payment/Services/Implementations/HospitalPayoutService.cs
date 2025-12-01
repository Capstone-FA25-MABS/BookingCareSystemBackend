using BookingCare.Services.Hospital;
using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Implementation of Hospital Payout Service
/// </summary>
public class HospitalPayoutService : IHospitalPayoutService
{
    private readonly IHospitalPayoutRepository _payoutRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly HospitalService.HospitalServiceClient _hospitalGrpcClient;
    private readonly ILogger<HospitalPayoutService> _logger;

    public HospitalPayoutService(
        IHospitalPayoutRepository payoutRepository,
        IPaymentRepository paymentRepository,
        IBankAccountRepository bankAccountRepository,
        HospitalService.HospitalServiceClient hospitalGrpcClient,
        ILogger<HospitalPayoutService> logger
    )
    {
        _payoutRepository = payoutRepository;
        _paymentRepository = paymentRepository;
        _bankAccountRepository = bankAccountRepository;
        _hospitalGrpcClient = hospitalGrpcClient;
        _logger = logger;
    }

    public async Task<PagedResult<HospitalPayoutResponse>> GetPayoutsAsync(PayoutQueryRequest query)
    {
        var pagedPayouts = await _payoutRepository.GetPayoutsAsync(query);

        var responses = pagedPayouts.Items.Select(MapToResponse).ToList();

        return new PagedResult<HospitalPayoutResponse>
        {
            Items = responses,
            TotalCount = pagedPayouts.TotalCount,
            PageNumber = pagedPayouts.PageNumber,
            PageSize = pagedPayouts.PageSize,
        };
    }

    public async Task<PayoutDetailsResponse> GetPayoutDetailsAsync(Guid payoutId)
    {
        var payout = await _payoutRepository.GetByIdAsync(payoutId);
        if (payout == null)
        {
            throw new KeyNotFoundException($"Payout with ID {payoutId} not found");
        }

        // Get all completed payments for this hospital in the period
        var payments = await _paymentRepository.GetCompletedPaymentsByHospitalAndPeriodAsync(
            payout.HospitalId,
            payout.PeriodStart,
            payout.PeriodEnd
        );

        var appointmentDetails = new List<PayoutAppointmentDetail>();
        // TODO: Need to fetch appointment and user details via gRPC
        // For now, return basic payment info

        return new PayoutDetailsResponse
        {
            Payout = MapToResponse(payout),
            Appointments = appointmentDetails,
        };
    }

    public async Task<List<HospitalPayoutResponse>> GeneratePayoutsAsync(
        GeneratePayoutsRequest request,
        Guid adminId
    )
    {
        var generatedPayouts = new List<HospitalPayoutEntity>();

        // Get hospitals to process
        var hospitalIds =
            request.HospitalIds
            ?? await GetHospitalsWithCompletedPaymentsAsync(
                request.PeriodStartDate,
                request.PeriodEndDate
            );

        // Fetch hospital names upfront for all hospitals
        var hospitalNames = await GetHospitalNamesAsync(hospitalIds);

        foreach (var hospitalId in hospitalIds)
        {
            // Check if payout already exists for this period
            var exists = await _payoutRepository.ExistsForPeriodAsync(
                hospitalId,
                request.PeriodStartDate,
                request.PeriodEndDate
            );

            if (exists)
            {
                continue; // Skip if already generated
            }

            // Calculate total amount from completed payments
            var (totalAmount, appointmentCount) = await CalculatePayoutAmountAsync(
                hospitalId,
                request.PeriodStartDate,
                request.PeriodEndDate
            );

            if (totalAmount <= 0 || appointmentCount == 0)
            {
                continue; // Skip if no payments
            }

            // Get default bank account for hospital
            var bankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(hospitalId);
            if (bankAccount == null)
            {
                throw new InvalidOperationException(
                    $"Hospital {hospitalId} does not have a default bank account"
                );
            }

            // Get hospital name
            var hospitalName = hospitalNames.TryGetValue(hospitalId, out var name)
                ? name
                : "Unknown Hospital";

            // Create payout record with hospital name
            var payout = new HospitalPayoutEntity
            {
                HospitalId = hospitalId,
                HospitalName = hospitalName,
                BankAccountId = bankAccount.Id,
                PeriodStart = request.PeriodStartDate,
                PeriodEnd = request.PeriodEndDate,
                TotalAmount = totalAmount,
                AppointmentCount = appointmentCount,
                Status = PayoutStatus.PENDING,
            };

            var created = await _payoutRepository.CreateAsync(payout);
            generatedPayouts.Add(created);
        }

        return generatedPayouts.Select(MapToResponse).ToList();
    }

    public async Task<HospitalPayoutResponse> MarkPayoutCompletedAsync(
        Guid payoutId,
        MarkPayoutCompletedRequest request,
        Guid adminId
    )
    {
        var payout = await _payoutRepository.GetByIdAsync(payoutId);
        if (payout == null)
        {
            throw new KeyNotFoundException($"Payout with ID {payoutId} not found");
        }

        if (payout.Status == PayoutStatus.COMPLETED)
        {
            throw new InvalidOperationException("Payout is already marked as completed");
        }

        payout.Status = PayoutStatus.COMPLETED;
        payout.ProcessedByAdminId = adminId;
        payout.ProcessedAt = DateTime.UtcNow;
        payout.Notes = request.Notes;

        var updated = await _payoutRepository.UpdateAsync(payout);

        return MapToResponse(updated);
    }

    public async Task<PayoutStatisticsResponse> GetStatisticsAsync()
    {
        var (pendingCount, pendingAmount, completedCount, completedAmount) =
            await _payoutRepository.GetStatisticsAsync();

        // Get unique hospital count with payouts
        // TODO: Implement if needed

        return new PayoutStatisticsResponse
        {
            TotalPendingPayouts = pendingCount,
            TotalPendingAmount = pendingAmount,
            TotalCompletedPayouts = completedCount,
            TotalCompletedAmount = completedAmount,
            TotalHospitals =
                0 // TODO
            ,
        };
    }

    public async Task<List<PendingHospitalInfoResponse>> GetHospitalsWithPendingPayoutsAsync(
        DateTime periodStart,
        DateTime periodEnd
    )
    {
        // Get all hospital IDs with completed payments in period
        var hospitalIds = await GetHospitalsWithCompletedPaymentsAsync(periodStart, periodEnd);

        // Fetch hospital names via gRPC
        var hospitalNames = await GetHospitalNamesAsync(hospitalIds);

        var pendingHospitals = new List<PendingHospitalInfoResponse>();

        foreach (var hospitalId in hospitalIds)
        {
            // Calculate payout amount and appointment count
            var (totalAmount, appointmentCount) = await CalculatePayoutAmountAsync(
                hospitalId,
                periodStart,
                periodEnd
            );

            // Check if hospital has bank account
            var bankAccount = await _bankAccountRepository.GetFirstByHospitalIdAsync(hospitalId);
            var hasBankAccount = bankAccount != null;

            // Get hospital name from fetched data
            var hospitalName = hospitalNames.TryGetValue(hospitalId, out var name)
                ? name
                : "Unknown Hospital";

            // Create response
            pendingHospitals.Add(
                new PendingHospitalInfoResponse
                {
                    HospitalId = hospitalId,
                    HospitalName = hospitalName,
                    TotalAmount = totalAmount,
                    AppointmentCount = appointmentCount,
                    HasBankAccount = hasBankAccount,
                }
            );
        }

        return pendingHospitals;
    }

    // Private helper methods

    private async Task<List<Guid>> GetHospitalsWithCompletedPaymentsAsync(
        DateTime periodStart,
        DateTime periodEnd
    )
    {
        // Get all completed appointment payments in period
        // Group by hospital ID
        var hospitalIds = await _paymentRepository.GetHospitalIdsWithCompletedPaymentsAsync(
            periodStart,
            periodEnd
        );
        return hospitalIds;
    }

    private async Task<(decimal TotalAmount, int AppointmentCount)> CalculatePayoutAmountAsync(
        Guid hospitalId,
        DateTime periodStart,
        DateTime periodEnd
    )
    {
        var payments = await _paymentRepository.GetCompletedPaymentsByHospitalAndPeriodAsync(
            hospitalId,
            periodStart,
            periodEnd
        );

        var totalAmount = payments.Sum(p => p.Amount);
        var appointmentCount = payments.Count(p => p.AppointmentId.HasValue);

        return (totalAmount, appointmentCount);
    }

    private HospitalPayoutResponse MapToResponse(HospitalPayoutEntity entity)
    {
        return new HospitalPayoutResponse
        {
            Id = entity.Id,
            HospitalId = entity.HospitalId,
            HospitalName = entity.HospitalName,
            BankAccountId = entity.BankAccountId,
            BankAccount = new BankAccountInfo
            {
                Id = entity.BankAccount.Id,
                BankCode = entity.BankAccount.BankCode,
                BankName = entity.BankAccount.BankName,
                AccountNumber = entity.BankAccount.AccountNumber,
                AccountName = entity.BankAccount.AccountName,
            },
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            TotalAmount = entity.TotalAmount,
            AppointmentCount = entity.AppointmentCount,
            Status = entity.Status,
            ProcessedByAdminId = entity.ProcessedByAdminId,
            ProcessedByAdminName = string.Empty, // TODO: Fetch from User service
            ProcessedAt = entity.ProcessedAt,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    /// <summary>
    /// Fetch hospital names via gRPC - optimized for performance
    /// </summary>
    private async Task<Dictionary<Guid, string>> GetHospitalNamesAsync(List<Guid> hospitalIds)
    {
        if (hospitalIds == null || !hospitalIds.Any())
        {
            return new Dictionary<Guid, string>();
        }

        try
        {
            var request = new GetHospitalNamesRequest();
            request.Ids.AddRange(hospitalIds.Select(id => id.ToString()));

            var response = await _hospitalGrpcClient.GetHospitalNamesAsync(request);

            return response.Hospitals.ToDictionary(h => Guid.Parse(h.Id), h => h.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching hospital names via gRPC for {Count} hospitals",
                hospitalIds.Count
            );
            // Return empty dictionary on error - will show "Unknown Hospital"
            return new Dictionary<Guid, string>();
        }
    }
}
