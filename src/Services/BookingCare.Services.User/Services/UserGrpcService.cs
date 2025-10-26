using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Utils;
using BookingCare.Services.User.Constants;
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
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
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
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
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
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
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
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
        }
    }

    public override async Task<Protos.DeleteUserResponse> DeleteUser(
        Protos.DeleteUserRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC DeleteUser called for ID: {UserId}", request.Id);

            if (!Guid.TryParse(request.Id, out var userId))
            {
                return new Protos.DeleteUserResponse
                {
                    Success = false,
                    Message = "Invalid user ID format"
                };
            }

            var result = await _userService.DeleteAsync(userId);

            if (result)
            {
                _logger.LogInformation("[UserGrpcService] User deleted successfully: {UserId}", userId);
                return new Protos.DeleteUserResponse
                {
                    Success = true,
                    Message = "User deleted successfully"
                };
            }
            else
            {
                return new Protos.DeleteUserResponse
                {
                    Success = true, // Consider it successful if already deleted
                    Message = "User not found or already deleted"
                };
            }
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error deleting user: {UserId}", request.Id);
            return new Protos.DeleteUserResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<Protos.UserBasicInfoResponse> GetUserBasicInfo(
        Protos.GetUserBasicInfoRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUserBasicInfo called for ID: {UserId}", request.Id);

            if (!Guid.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var userBasicInfo = await _userService.GetBasicInfoByIdAsync(userId);
            if (userBasicInfo == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {userId} not found"));
            }

            var response = MapToGrpcUserBasicInfoResponse(userBasicInfo);
            _logger.LogInformation("[UserGrpcService] User basic info retrieved successfully: {UserId}", userId);

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error getting user basic info: {UserId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
        }
    }

    public override async Task<Protos.UsersBasicInfoResponse> GetUsersBasicInfo(
        Protos.GetUsersBasicInfoRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUsersBasicInfo called for {Count} user IDs", request.Ids.Count);

            var userIds = new List<Guid>();
            foreach (var idStr in request.Ids)
            {
                if (!Guid.TryParse(idStr, out var id))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid user ID format: {idStr}"));
                }
                userIds.Add(id);
            }

            var users = await _userService.GetUsersBasicInfoByIdsAsync(userIds);
            var response = new Protos.UsersBasicInfoResponse();

            foreach (var user in users)
            {
                response.Users.Add(MapToGrpcUserBasicInfoResponse(user));
            }

            _logger.LogInformation("[UserGrpcService] Retrieved {Count} users basic info for batch request", users.Count);
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error in GetUsersBasicInfo");
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
        }
    }

    /// <summary>
    /// NEW: Get user display info in consistent format with AuthService (for review patient info)
    /// </summary>
    public override async Task<Protos.UserDisplayInfoResponse> GetUserDisplayInfo(
        Protos.GetUserDisplayInfoRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUserDisplayInfo called for ID: {UserId}", request.Id);

            if (!Guid.TryParse(request.Id, out var userId))
            {
                return new Protos.UserDisplayInfoResponse
                {
                    Id = request.Id,
                    Found = false
                };
            }

            var userBasicInfo = await _userService.GetBasicInfoByIdAsync(userId);

            if (userBasicInfo == null)
            {
                _logger.LogInformation("[UserGrpcService] User display info not found: {UserId}", userId);
                return new Protos.UserDisplayInfoResponse
                {
                    Id = request.Id,
                    Found = false
                };
            }

            var response = new Protos.UserDisplayInfoResponse
            {
                Id = userBasicInfo.Id.ToString(),
                Email = userBasicInfo.Email,
                FullName = $"{userBasicInfo.FirstName} {userBasicInfo.LastName}".Trim(),
                AvatarUrl = userBasicInfo.AvatarUrl,
                Found = true
            };

            _logger.LogInformation("[UserGrpcService] User display info retrieved successfully: {UserId}", userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error getting user display info: {UserId}", request.Id);
            return new Protos.UserDisplayInfoResponse
            {
                Id = request.Id,
                Found = false
            };
        }
    }

    /// <summary>
    /// NEW: Get multiple users display info in consistent format with AuthService (for review patient info batch)
    /// </summary>
    public override async Task<Protos.UsersDisplayInfoResponse> GetUsersDisplayInfo(
        Protos.GetUsersDisplayInfoRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[UserGrpcService] gRPC GetUsersDisplayInfo called for {Count} user IDs", request.Ids.Count);

            var response = new Protos.UsersDisplayInfoResponse();

            if (!request.Ids.Any())
            {
                _logger.LogInformation("[UserGrpcService] No user IDs provided for GetUsersDisplayInfo");
                return response;
            }

            var userIds = new List<Guid>();
            var invalidIds = new List<string>();

            // Parse and validate user IDs
            foreach (var idStr in request.Ids)
            {
                if (Guid.TryParse(idStr, out var id))
                {
                    userIds.Add(id);
                }
                else
                {
                    invalidIds.Add(idStr);
                }
            }

            // Get users from service
            var users = await _userService.GetUsersBasicInfoByIdsAsync(userIds);
            var userDict = users.ToDictionary(u => u.Id, u => u);

            // Map found users
            foreach (var userId in userIds)
            {
                if (userDict.TryGetValue(userId, out var user))
                {
                    response.Users.Add(new Protos.UserDisplayInfoResponse
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        AvatarUrl = user.AvatarUrl,
                        Found = true
                    });
                }
                else
                {
                    response.Users.Add(new Protos.UserDisplayInfoResponse
                    {
                        Id = userId.ToString(),
                        Found = false
                    });
                }
            }

            // Add invalid IDs as not found - only if there are any invalid IDs
            if (invalidIds.Count > 0)
            {
                foreach (var invalidId in invalidIds)
                {
                    response.Users.Add(new Protos.UserDisplayInfoResponse
                    {
                        Id = invalidId,
                        Found = false
                    });
                }
            }

            _logger.LogInformation("[UserGrpcService] Retrieved {Count} users display info - Found: {FoundCount}, NotFound: {NotFoundCount}",
                request.Ids.Count,
                response.Users.Count(u => u.Found),
                response.Users.Count(u => !u.Found));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserGrpcService] Error in GetUsersDisplayInfo");
            throw new RpcException(new Status(StatusCode.Internal, ErrorMessages.InternalServerError));
        }
    }

    // Helper methods for mapping
    private static Protos.UserResponse MapToGrpcUserResponse(UserResponse user)
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

    private static Protos.UserBasicInfoResponse MapToGrpcUserBasicInfoResponse(UserBasicInfoDto userBasicInfo)
    {
        return new Protos.UserBasicInfoResponse
        {
            Id = userBasicInfo.Id.ToString(),
            Email = userBasicInfo.Email,
            Phone = userBasicInfo.Phone,
            FirstName = userBasicInfo.FirstName,
            LastName = userBasicInfo.LastName,
            AvatarUrl = userBasicInfo.AvatarUrl
        };
    }
}
