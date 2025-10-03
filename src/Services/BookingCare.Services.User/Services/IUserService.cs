using BookingCare.Services.User.Models.DTOs;

namespace BookingCare.Services.User.Services;

public interface IUserService
{
    // Basic CRUD operations
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task<UserResponse?> GetByAccountIdAsync(Guid accountId);
    Task<UserResponse> CreateAsync(CreateUserRequest createUserRequest);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false);
    Task<UserResponse> UpdateByAccountIdAsync(Guid accountId, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false);
    Task<bool> DeleteAsync(Guid id);

    // Query operations
    Task<UserListResponse> GetUsersAsync(UserQueryRequest query);
    Task<UserSearchResponse> SearchUsersAsync(string searchTerm, int limit = 10);

    // Batch operations for performance optimization
    Task<List<UserBasicInfoResponse>> GetUsersByAccountIdsAsync(List<Guid> accountIds);

}
