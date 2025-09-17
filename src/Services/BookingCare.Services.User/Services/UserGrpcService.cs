using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Utils;
using Grpc.Core;

namespace BookingCare.Services.User.Services;

public class UserGrpcService : Protos.UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(
        IUserService userService,
        ILogger<UserGrpcService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<Protos.UserResponse> GetUser(
        Protos.GetUserRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUser called for ID: {UserId}", request.Id);

            if (!Guid.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {userId} not found"));
            }

            var response = MapToGrpcUserResponse(user);
            _logger.LogInformation("[UserGrpcService] User retrieved successfully: {UserId}", userId);

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error getting user: {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.UserResponse> GetUserByAccountId(
        Protos.GetUserByAccountIdRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUserByAccountId called for AccountId: {AccountId}", request.AccountId);

            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var user = await _userService.GetByAccountIdAsync(accountId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with AccountId {accountId} not found"));
            }

            var response = MapToGrpcUserResponse(user);
            _logger.LogInformation("[UserGrpcService] User retrieved by AccountId successfully: {AccountId}", accountId);

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error getting user by AccountId: {AccountId}", request.AccountId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.UserResponse> CreateUser(
        Protos.CreateUserRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC CreateUser called for email: {Email}", request.Email);

            if (!Guid.TryParse(request.AccountId, out var accountId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid account ID format"));
            }

            var createRequest = new CreateUserRequest
            {
                AccountId = accountId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = UserParsingUtils.ParseGender(request.Gender),
                DateOfBirth = UserParsingUtils.ParseDateOfBirth(request.DateOfBirth),
                Address = !string.IsNullOrEmpty(request.Address) ? request.Address : null,
                Phone = !string.IsNullOrEmpty(request.Phone) ? request.Phone : null,
                AvatarUrl = !string.IsNullOrEmpty(request.AvatarUrl) ? request.AvatarUrl : null
            };

            var user = await _userService.CreateAsync(createRequest);
            var response = MapToGrpcUserResponse(user);

            _logger.LogInformation("[UserGrpcService] User created successfully: {UserId}", user.Id);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error creating user with email: {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Protos.UserBatchResponse> GetUsersByAccountIds(
        Protos.GetUsersByAccountIdsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUsersByAccountIds called for {Count} account IDs", request.AccountIds.Count);

            // Validate account IDs
            var accountIds = new List<Guid>();
            foreach (var accountIdString in request.AccountIds)
            {
                if (!Guid.TryParse(accountIdString, out var accountId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid account ID format: {accountIdString}"));
                }
                accountIds.Add(accountId);
            }

            // Get users batch - now returns List<UserBasicInfoResponse> directly
            var users = await _userService.GetUsersByAccountIdsAsync(accountIds);

            // Map to gRPC response
            var grpcResponse = new Protos.UserBatchResponse();

            foreach (var user in users)
            {
                grpcResponse.Users.Add(new Protos.UserBasicInfo
                {
                    AccountId = user.AccountId.ToString(),
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl
                });
            }

            _logger.LogInformation("[UserGrpcService] Retrieved {Count} users for batch request", users.Count);
            return grpcResponse;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error getting users by account IDs batch");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    // Helper methods for mapping
    private Protos.UserResponse MapToGrpcUserResponse(UserResponse user)
    {
        return new Protos.UserResponse
        {
            Id = user.Id.ToString(),
            AccountId = user.AccountId.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Gender = user.Gender?.ToString() ?? string.Empty,
            DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty,
            Address = user.Address ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
        };
    }
}
