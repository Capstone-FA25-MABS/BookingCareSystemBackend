using AutoMapper;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using BookingCare.Services.User.Repositories;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.User.Services;

public class UserService : BaseService, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        IEventBus eventBus,
        ILogger<UserService> logger) : base(logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _eventBus = eventBus;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(id, nameof(id));
            LogDebug("Getting user by ID: {UserId}", null, id);

            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? _mapper.Map<UserResponse>(user) : null;

        }, "GetUserById");
    }

    public async Task<UserResponse?> GetByAccountIdAsync(Guid accountId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(accountId, nameof(accountId));
            LogDebug("Getting user by account ID: {AccountId}", null, accountId);

            var user = await _userRepository.GetByAccountIdAsync(accountId);
            return user != null ? _mapper.Map<UserResponse>(user) : null;

        }, "GetUserByAccountId");
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest createUserRequest)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Creating user with email: {Email}", null, createUserRequest.Email);
            ValidateRequired(createUserRequest, nameof(createUserRequest));

            var userEntity = _mapper.Map<UserEntity>(createUserRequest);
            userEntity.AvatarUrl = createUserRequest.AvatarUrl ?? "https://bookingcaree.com/user-avatar-default.png";

            var createdUser = await _userRepository.CreateAsync(userEntity);

            LogInfo("User created successfully with ID: {UserId}", null, createdUser.Id);
            return _mapper.Map<UserResponse>(createdUser);

        }, "CreateUser");
    }

    public async Task<UserResponse> UpdateByAccountIdAsync(Guid accountId, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating user by AccountId: {AccountId}", null, accountId);
            ValidateGuid(accountId, nameof(accountId));

            var existingUser = await _userRepository.GetByAccountIdAsync(accountId);
            if (existingUser == null)
            {
                throw new UserNotFoundException(accountId);
            }

            return await UpdateUserInternalAsync(existingUser, updateUserRequest, emailConfirmed, phoneConfirmed);
        }, "UpdateUserByAccountId");
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest updateUserRequest, bool emailConfirmed = false, bool phoneConfirmed = false)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating user with ID: {UserId}", null, id);
            ValidateGuid(id, nameof(id));

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                throw new UserNotFoundException(id);
            }

            return await UpdateUserInternalAsync(existingUser, updateUserRequest, emailConfirmed, phoneConfirmed);
        }, "UpdateUser");
    }

    private async Task<UserResponse> UpdateUserInternalAsync(UserEntity existingUser, UpdateUserRequest updateUserRequest, bool emailConfirmed, bool phoneConfirmed)
    {
        ValidateRequired(updateUserRequest, nameof(updateUserRequest));

        LogInfo("Updating user ID: {UserId}, EmailConfirmed: {EmailConfirmed}, PhoneConfirmed: {PhoneConfirmed}",
            null, existingUser.Id, emailConfirmed, phoneConfirmed);

        // Get original values for comparison
        var originalEmail = existingUser.Email;
        var originalPhone = existingUser.Phone;

        // Business rule: At least one of email or phone must be confirmed
        if (!emailConfirmed && !phoneConfirmed)
        {
            LogError(new InvalidOperationException("Business rule violation: At least one of email or phone must be confirmed"),
                "At least one of email or phone must be confirmed");
            throw new InvalidOperationException("Business rule violation: At least one of email or phone must be confirmed");
        }

        // Handle email update
        string? newEmail = null;
        bool needsEmailSync = false;

        if (!string.IsNullOrEmpty(updateUserRequest.Email) && updateUserRequest.Email != originalEmail)
        {
            if (emailConfirmed)
            {
                // Silently ignore attempt to update confirmed email
                LogWarning("Ignoring attempt to update confirmed email for user: {UserId} - Email will not be changed",
                    null, existingUser.Id);
            }
            else
            {
                // Email is not confirmed, can update
                LogInfo("Checking if new email already exists: {Email}", null, updateUserRequest.Email);

                var emailExists = await _userRepository.EmailExistsAsync(updateUserRequest.Email);
                if (emailExists)
                {
                    LogWarning("Email already exists: {Email}", null, updateUserRequest.Email);
                    throw new EmailAlreadyExistsException(updateUserRequest.Email);
                }

                newEmail = updateUserRequest.Email;
                needsEmailSync = true;
                LogInfo("Email update will require SAGA synchronization", null);
            }
        }

        // Handle phone update
        string? newPhone = null;
        bool needsPhoneSync = false;

        if (!string.IsNullOrEmpty(updateUserRequest.Phone) && updateUserRequest.Phone != originalPhone)
        {
            if (phoneConfirmed)
            {
                // Silently ignore attempt to update confirmed phone
                LogWarning("Ignoring attempt to update confirmed phone for user: {UserId} - Phone will not be changed",
                    null, existingUser.Id);
            }
            else
            {
                // Phone is not confirmed, can update
                LogInfo("Checking if new phone already exists: {Phone}", null, updateUserRequest.Phone);

                var phoneExists = await _userRepository.PhoneExistsAsync(updateUserRequest.Phone);
                if (phoneExists)
                {
                    LogWarning("Phone already exists: {Phone}", null, updateUserRequest.Phone);
                    throw new PhoneAlreadyExistsException(updateUserRequest.Phone);
                }

                newPhone = updateUserRequest.Phone;
                needsPhoneSync = true;
                LogInfo("Phone update will require SAGA synchronization", null);
            }
        }

        bool needsSagaSync = needsEmailSync || needsPhoneSync;

        // If SAGA synchronization is needed, publish event
        if (needsSagaSync)
        {
            var correlationId = Guid.NewGuid().ToString();
            LogInfo("Publishing UserEmailPhoneSyncRequestedEvent with CorrelationId: {CorrelationId}", null, correlationId);

            var syncEvent = new UserEmailPhoneSyncRequestedEvent
            {
                AccountId = existingUser.AccountId,
                UserId = existingUser.Id,
                OriginalEmail = originalEmail,
                OriginalPhone = originalPhone,
                NewEmail = newEmail,
                NewPhone = newPhone,
                CorrelationId = correlationId,
                RequestedAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(syncEvent);
            LogInfo("UserEmailPhoneSyncRequestedEvent published successfully", null);
        }

        // Track if any changes were made
        bool hasChanges = false;

        // Apply updates to user entity only if values are different
        if (!string.IsNullOrEmpty(updateUserRequest.FirstName) && updateUserRequest.FirstName != existingUser.FirstName)
        {
            if (updateUserRequest.FirstName.Trim().Length < 2)
            {
                LogError(new InvalidOperationException("First name must be at least 2 characters"),
                    "First name must be at least 2 characters");
                throw new InvalidOperationException("First name must be at least 2 characters");
            }
            existingUser.FirstName = updateUserRequest.FirstName.Trim();
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(updateUserRequest.LastName) && updateUserRequest.LastName != existingUser.LastName)
        {
            if (updateUserRequest.LastName.Trim().Length < 2)
            {
                LogError(new InvalidOperationException("Last name must be at least 2 characters"),
                    "Last name must be at least 2 characters");
                throw new InvalidOperationException("Last name must be at least 2 characters");
            }
            existingUser.LastName = updateUserRequest.LastName.Trim();
            hasChanges = true;
        }

        // Only update email if it's not confirmed or if it's the same value
        if (!string.IsNullOrEmpty(newEmail))
        {
            existingUser.Email = newEmail;
            hasChanges = true;
        }

        // Only update phone if it's not confirmed or if it's the same value
        if (!string.IsNullOrEmpty(newPhone))
        {
            existingUser.Phone = newPhone;
            hasChanges = true;
        }

        if (updateUserRequest.Gender.HasValue && updateUserRequest.Gender != existingUser.Gender)
        {
            existingUser.Gender = updateUserRequest.Gender.Value;
            hasChanges = true;
        }

        if (updateUserRequest.DateOfBirth.HasValue && updateUserRequest.DateOfBirth != existingUser.DateOfBirth)
        {
            // Validate age >= 18
            var age = DateTime.UtcNow.Year - updateUserRequest.DateOfBirth.Value.Year;
            if (updateUserRequest.DateOfBirth.Value > DateTime.UtcNow.AddYears(-age)) age--;

            if (age < 18)
            {
                LogError(new InvalidOperationException("User must be at least 18 years old"),
                    "User must be at least 18 years old");
                throw new InvalidOperationException("User must be at least 18 years old");
            }

            existingUser.DateOfBirth = updateUserRequest.DateOfBirth;
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(updateUserRequest.Address) && updateUserRequest.Address != existingUser.Address)
        {
            if (updateUserRequest.Address.Trim().Length < 5)
            {
                LogError(new InvalidOperationException("Address must be at least 5 characters"),
                    "Address must be at least 5 characters");
                throw new InvalidOperationException("Address must be at least 5 characters");
            }
            existingUser.Address = updateUserRequest.Address.Trim();
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(updateUserRequest.AvatarUrl) && updateUserRequest.AvatarUrl != existingUser.AvatarUrl)
        {
            existingUser.AvatarUrl = updateUserRequest.AvatarUrl;
            hasChanges = true;
        }

        // Only update if there are actual changes
        if (!hasChanges)
        {
            LogInfo("No changes detected for user: {UserId}, skipping database update", null, existingUser.Id);
            return _mapper.Map<UserResponse>(existingUser);
        }

        // Save to database
        var updatedUser = await _userRepository.UpdateAsync(existingUser);

        LogInfo("User updated successfully with ID: {UserId}", null, existingUser.Id);
        return _mapper.Map<UserResponse>(updatedUser);
    }

    public async Task<UserListResponse> GetUsersAsync(UserQueryRequest query)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequired(query, nameof(query));
            LogInfo("Getting users - Page: {Page}, PageSize: {PageSize}", null, query.PageNumber, query.PageSize);

            var (users, totalCount) = await _userRepository.GetUsersAsync(query);
            var userResponses = _mapper.Map<List<UserResponse>>(users);

            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var response = new UserListResponse
            {
                Users = userResponses,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };

            LogInfo("Retrieved {Count} users out of {Total}", null, userResponses.Count, totalCount);
            return response;

        }, "GetUsers");
    }

    public async Task<UserSearchResponse> SearchUsersAsync(string searchTerm, int limit = 10)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequiredString(searchTerm, nameof(searchTerm));
            LogInfo("Searching users with term: {SearchTerm}, limit: {Limit}", null, searchTerm, limit);

            var users = await _userRepository.SearchUsersAsync(searchTerm, limit);
            var userResponses = _mapper.Map<List<UserResponse>>(users);

            return new UserSearchResponse
            {
                Users = userResponses,
                TotalFound = userResponses.Count,
                SearchTerm = searchTerm,
                Limit = limit
            };

        }, "SearchUsers");
    }

    public async Task<List<UserBasicInfoResponse>> GetUsersByAccountIdsAsync(List<Guid> accountIds)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequired(accountIds, nameof(accountIds));

            LogInfo("Getting users by account IDs batch - Count: {Count}", null, accountIds.Count);

            // Repository already returns optimized DTOs directly from database
            var users = await _userRepository.GetUsersByAccountIdsAsync(accountIds);

            LogInfo("Retrieved {Count} users for batch request", null, users.Count);
            return users;

        }, "GetUsersByAccountIds");
    }

    /// <summary>
    /// Delete user by ID
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Deleting user: {UserId}", null, id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                LogWarning("User not found for deletion: {UserId}", null, id);
                return true; // Consider success if already deleted
            }

            var result = await _userRepository.DeleteAsync(user);
            if (result)
            {
                LogInfo("User deleted successfully: {UserId}", null, id);
            }
            else
            {
                LogWarning("Failed to delete user: {UserId}", null, id);
            }

            return result;
        }, "DeleteUser");
    }

}