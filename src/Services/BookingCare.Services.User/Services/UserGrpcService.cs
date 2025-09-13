using BookingCare.Services.User.Protos;
using Grpc.Core;

namespace BookingCare.Services.User.Services;

public class UserGrpcService : UserService.UserServiceBase
{
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(ILogger<UserGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task<CreateUserProfileResponse> CreateUserProfile(
        CreateUserProfileRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Creating user profile for UserId: {UserId}", request.UserId);

            // TODO: Implement actual user profile creation logic
            // For now, simulate the operation
            await Task.Delay(100); // Simulate DB operation

            var profileId = Guid.NewGuid().ToString();

            _logger.LogInformation("User profile created successfully. ProfileId: {ProfileId}, UserId: {UserId}", 
                profileId, request.UserId);

            return new CreateUserProfileResponse
            {
                Success = true,
                Message = "User profile created successfully",
                ProfileId = profileId,
                UserId = request.UserId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user profile for UserId: {UserId}", request.UserId);
            return new CreateUserProfileResponse
            {
                Success = false,
                Message = $"Failed to create user profile: {ex.Message}",
                UserId = request.UserId
            };
        }
    }

    public override async Task<DeleteUserProfileResponse> DeleteUserProfile(
        DeleteUserProfileRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Deleting user profile. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);

            // TODO: Implement actual user profile deletion logic
            await Task.Delay(50); // Simulate DB operation

            _logger.LogInformation("User profile deleted successfully. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);

            return new DeleteUserProfileResponse
            {
                Success = true,
                Message = "User profile deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user profile. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);
            return new DeleteUserProfileResponse
            {
                Success = false,
                Message = $"Failed to delete user profile: {ex.Message}"
            };
        }
    }

    public override async Task<GetUserProfileResponse> GetUserProfile(
        GetUserProfileRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting user profile for UserId: {UserId}", request.UserId);

            // TODO: Implement actual user profile retrieval logic
            await Task.Delay(50); // Simulate DB operation

            // For demo purposes, return a mock profile
            var profile = new UserProfile
            {
                ProfileId = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+1234567890",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return new GetUserProfileResponse
            {
                Success = true,
                Message = "User profile retrieved successfully",
                Profile = profile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for UserId: {UserId}", request.UserId);
            return new GetUserProfileResponse
            {
                Success = false,
                Message = $"Failed to get user profile: {ex.Message}"
            };
        }
    }

    public override async Task<UpdateUserProfileResponse> UpdateUserProfile(
        UpdateUserProfileRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Updating user profile. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);

            // TODO: Implement actual user profile update logic
            await Task.Delay(100); // Simulate DB operation

            _logger.LogInformation("User profile updated successfully. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);

            return new UpdateUserProfileResponse
            {
                Success = true,
                Message = "User profile updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile. ProfileId: {ProfileId}, UserId: {UserId}", 
                request.ProfileId, request.UserId);
            return new UpdateUserProfileResponse
            {
                Success = false,
                Message = $"Failed to update user profile: {ex.Message}"
            };
        }
    }
}
