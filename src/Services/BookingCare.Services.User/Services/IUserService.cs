using BookingCare.Services.User.Models.DTOs;
using BookingCare.Shared.Common.Interfaces;

namespace BookingCare.Services.User.Services;

public interface IUserService : IAvatarService
{
    // Basic CRUD operations
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task<UserResponse?> GetByAccountIdAsync(Guid accountId);
    Task<UserResponse> CreateAsync(CreateUserRequest createUserRequest);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false);
    Task<UserResponse> UpdateByAccountIdAsync(Guid accountId, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false);
    Task<bool> DeleteAsync(Guid id);

    // Performance-optimized operations
    Task<UserBasicInfoDto?> GetBasicInfoByIdAsync(Guid id);
    Task<List<UserBasicInfoDto>> GetUsersBasicInfoByIdsAsync(List<Guid> ids);

    // Query operations
    Task<UserListResponse> GetUsersAsync(UserQueryRequest query);
    Task<UserSearchResponse> SearchUsersAsync(string searchTerm, int limit = 10);

    // Batch operations for performance optimization
    Task<List<UserBasicInfoResponse>> GetUsersByAccountIdsAsync(List<Guid> accountIds);

}
