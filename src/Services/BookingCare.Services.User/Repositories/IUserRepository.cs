using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;

namespace BookingCare.Services.User.Repositories;

public interface IUserRepository
{
    // Basic CRUD operations
    Task<UserEntity?> GetByIdAsync(Guid id);
    Task<UserEntity?> GetByAccountIdAsync(Guid accountId);
    Task<UserEntity> CreateAsync(UserEntity user);
    Task<UserEntity> UpdateAsync(UserEntity user);

    // Query operations
    Task<(List<UserEntity> Users, int TotalCount)> GetUsersAsync(UserQueryRequest query);
    Task<List<UserEntity>> SearchUsersAsync(string searchTerm, int limit = 10);

}
