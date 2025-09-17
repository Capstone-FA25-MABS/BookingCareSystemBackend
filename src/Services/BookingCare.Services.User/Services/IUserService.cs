using BookingCare.Services.User.Models.DTOs;

namespace BookingCare.Services.User.Services;

public interface IUserService
{
    // Basic CRUD operations
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task<UserResponse?> GetByAccountIdAsync(Guid accountId);
    Task<UserResponse> CreateAsync(CreateUserRequest createUserRequest);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest updateUserRequest);

    // Query operations
    Task<UserListResponse> GetUsersAsync(UserQueryRequest query);
    Task<UserSearchResponse> SearchUsersAsync(string searchTerm, int limit = 10);

    // Batch operations for performance optimization
    Task<List<UserBasicInfoResponse>> GetUsersByAccountIdsAsync(List<Guid> accountIds);

}
