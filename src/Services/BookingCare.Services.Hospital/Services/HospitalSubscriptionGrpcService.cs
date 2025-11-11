using BookingCare.Services.Hospital.Enums;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using Grpc.Core;

namespace BookingCare.Services.Hospital.Services;

public class HospitalSubscriptionGrpcService : HospitalSubscriptionGrpc.HospitalSubscriptionGrpcBase
{
    private readonly IHospitalSubscriptionService _subscriptionService;
    private readonly ILogger<HospitalSubscriptionGrpcService> _logger;

    public HospitalSubscriptionGrpcService(
        IHospitalSubscriptionService subscriptionService,
        ILogger<HospitalSubscriptionGrpcService> logger
    )
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new hospital subscription via gRPC
    /// </summary>
    public override async Task<HospitalSubscriptionGrpcResponse> CreateHospitalSubscription(
        CreateHospitalSubscriptionGrpcRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "gRPC: Creating hospital subscription for HospitalId: {HospitalId}, SubscriptionPlanId: {SubscriptionId}",
                request.HospitalId,
                request.SubscriptionId
            );

            // Validate request
            if (
                string.IsNullOrWhiteSpace(request.HospitalId)
                || string.IsNullOrWhiteSpace(request.SubscriptionId)
            )
            {
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        "HospitalId and SubscriptionId are required"
                    )
                );
            }

            // Parse GUIDs
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid HospitalId format")
                );
            }

            if (!Guid.TryParse(request.SubscriptionId, out var subscriptionId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid SubscriptionId format")
                );
            }

            // Parse dates if provided, otherwise use defaults (will be auto-calculated by service)
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrWhiteSpace(request.StartDate))
            {
                if (!DateTime.TryParse(request.StartDate, out var parsedStartDate))
                {
                    throw new RpcException(
                        new Status(
                            StatusCode.InvalidArgument,
                            "Invalid StartDate format. Expected ISO 8601 format"
                        )
                    );
                }
                startDate = parsedStartDate;
            }

            if (!string.IsNullOrWhiteSpace(request.EndDate))
            {
                if (!DateTime.TryParse(request.EndDate, out var parsedEndDate))
                {
                    throw new RpcException(
                        new Status(
                            StatusCode.InvalidArgument,
                            "Invalid EndDate format. Expected ISO 8601 format"
                        )
                    );
                }
                endDate = parsedEndDate;
            }

            // Create the request DTO
            var createRequest = new CreateHospitalSubscriptionRequest
            {
                HospitalId = hospitalId,
                SubscriptionId = subscriptionId,
                StartDate = startDate ?? DateTime.UtcNow, // Default to now if not provided
                EndDate = endDate ?? DateTime.UtcNow.AddMonths(1), // Default to 1 month if not provided (will be recalculated by service based on billing cycle)
            };

            // Call the service method
            var response = await _subscriptionService.CreateAsync(createRequest);

            _logger.LogInformation(
                "gRPC: Successfully created hospital subscription with ID: {SubscriptionId}",
                response.HospitalSubscriptionId
            );

            // Map to gRPC response
            return MapToGrpcResponse(response);
        }
        catch (HospitalOperationException ex)
        {
            _logger.LogError(ex, "gRPC: Hospital operation error while creating subscription");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (HospitalNotFoundException ex)
        {
            _logger.LogError(ex, "gRPC: Hospital not found");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (SubscriptionPlanNotFoundException ex)
        {
            _logger.LogError(ex, "gRPC: Subscription plan not found");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error creating hospital subscription");
            throw new RpcException(
                new Status(StatusCode.Internal, "An error occurred while creating the subscription")
            );
        }
    }

    /// <summary>
    /// Upgrades an existing hospital subscription to a new plan via gRPC
    /// </summary>
    public override async Task<HospitalSubscriptionGrpcResponse> UpgradeHospitalSubscription(
        UpgradeHospitalSubscriptionGrpcRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "gRPC: Upgrading subscription {CurrentSubscriptionId} to plan {NewPlanId}",
                request.CurrentSubscriptionId,
                request.NewSubscriptionPlanId
            );

            // Validate request
            if (
                string.IsNullOrWhiteSpace(request.CurrentSubscriptionId)
                || string.IsNullOrWhiteSpace(request.NewSubscriptionPlanId)
            )
            {
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        "CurrentSubscriptionId and NewSubscriptionPlanId are required"
                    )
                );
            }

            // Parse GUIDs
            if (!Guid.TryParse(request.CurrentSubscriptionId, out var currentSubscriptionId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid CurrentSubscriptionId format")
                );
            }

            if (!Guid.TryParse(request.NewSubscriptionPlanId, out var newSubscriptionPlanId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid NewSubscriptionPlanId format")
                );
            }

            // Call the service method
            var response = await _subscriptionService.UpgradeSubscriptionAsync(
                currentSubscriptionId,
                newSubscriptionPlanId
            );

            _logger.LogInformation(
                "gRPC: Successfully upgraded subscription to new ID: {NewSubscriptionId}",
                response.HospitalSubscriptionId
            );

            // Map to gRPC response
            return MapToGrpcResponse(response);
        }
        catch (HospitalOperationException ex)
        {
            _logger.LogError(ex, "gRPC: Hospital operation error while upgrading subscription");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (SubscriptionPlanNotFoundException ex)
        {
            _logger.LogError(ex, "gRPC: Subscription plan not found");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC: Error upgrading hospital subscription");
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "An error occurred while upgrading the subscription"
                )
            );
        }
    }

    /// <summary>
    /// Maps the HospitalSubscriptionResponse to gRPC response format
    /// </summary>
    private static HospitalSubscriptionGrpcResponse MapToGrpcResponse(
        HospitalSubscriptionResponse response
    )
    {
        var grpcResponse = new HospitalSubscriptionGrpcResponse
        {
            HospitalSubscriptionId = response.HospitalSubscriptionId.ToString(),
            HospitalId = response.HospitalId.ToString(),
            SubscriptionId = response.SubscriptionId.ToString(),
            StartDate = response.StartDate.ToString("O"), // ISO 8601 format
            EndDate = response.EndDate.ToString("O"),
            Status = response.Status.ToString(),
            CreatedAt = response.CreatedAt.ToString("O"),
            UpdatedAt = response.UpdatedAt.ToString("O"),
            IsActive = response.IsActive,
            DaysRemaining = response.DaysRemaining,
        };

        // Map hospital info if available
        if (response.Hospital != null)
        {
            grpcResponse.Hospital = new HospitalBasicInfoForSubscription
            {
                Id = response.Hospital.Id.ToString(),
                Name = response.Hospital.Name ?? string.Empty,
                Email = string.Empty, // HospitalSimpleResponse doesn't have Email
                Phone =
                    string.Empty // HospitalSimpleResponse doesn't have Phone
                ,
            };
        }

        // Map subscription plan info if available
        if (response.SubscriptionPlan != null)
        {
            grpcResponse.SubscriptionPlan = new SubscriptionPlanInfo
            {
                Id = response.SubscriptionPlan.Id.ToString(),
                Name = response.SubscriptionPlan.Name ?? string.Empty,
                BillingCycle = response.SubscriptionPlan.BillingCycle ?? string.Empty,
                Price = (double)response.SubscriptionPlan.Price,
                MaxDoctors = response.SubscriptionPlan.MaxDoctors ?? 0,
                MaxAppointments = response.SubscriptionPlan.MaxAppointments ?? 0,
            };

            // Add features if available
            if (!string.IsNullOrWhiteSpace(response.SubscriptionPlan.Features))
            {
                // Split features string by comma or newline
                var features = response
                    .SubscriptionPlan.Features.Split(
                        new[] { ',', '\n' },
                        StringSplitOptions.RemoveEmptyEntries
                    )
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrWhiteSpace(f));

                grpcResponse.SubscriptionPlan.Features.AddRange(features);
            }
        }

        return grpcResponse;
    }
}
