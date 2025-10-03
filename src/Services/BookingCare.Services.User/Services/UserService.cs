using AutoMapper;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using BookingCare.Services.User.Repositories;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Enums;

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

        // Business rule validation
        ValidateBusinessRules(emailConfirmed, phoneConfirmed);

        // Get original values for comparison
        var originalEmail = existingUser.Email;
        var originalPhone = existingUser.Phone;

        // Handle email and phone updates
        var (newEmail, needsEmailSync) = await HandleEmailUpdateAsync(updateUserRequest.Email, originalEmail, emailConfirmed, existingUser.Id);
        var (newPhone, needsPhoneSync) = await HandlePhoneUpdateAsync(updateUserRequest.Phone, originalPhone, phoneConfirmed, existingUser.Id);

        // Publish SAGA event if needed
        await PublishSagaSyncEventIfNeededAsync(needsEmailSync, needsPhoneSync, existingUser, originalEmail, originalPhone, newEmail, newPhone);

        // Apply all field updates
        var hasChanges = ApplyFieldUpdates(existingUser, updateUserRequest, newEmail, newPhone);

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

    /// <summary>
    /// Validate business rules for user update
    /// </summary>
    private void ValidateBusinessRules(bool emailConfirmed, bool phoneConfirmed)
    {
        if (emailConfirmed || phoneConfirmed) return;

        LogError(new InvalidOperationException("Business rule violation: At least one of email or phone must be confirmed"),
            "At least one of email or phone must be confirmed");
        throw new InvalidOperationException("Business rule violation: At least one of email or phone must be confirmed");
    }

    /// <summary>
    /// Handle email update validation
    /// </summary>
    private async Task<(string? newEmail, bool needsSync)> HandleEmailUpdateAsync(string? requestedEmail, string? originalEmail, bool emailConfirmed, Guid userId)
    {
        if (string.IsNullOrEmpty(requestedEmail) || requestedEmail == originalEmail)
        {
            return (null, false);
        }

        if (emailConfirmed)
        {
            LogWarning("Ignoring attempt to update confirmed email for user: {UserId} - Email will not be changed", null, userId);
            return (null, false);
        }

        LogInfo("Checking if new email already exists: {Email}", null, requestedEmail);

        var emailExists = await _userRepository.EmailExistsAsync(requestedEmail);
        if (emailExists)
        {
            LogWarning("Email already exists: {Email}", null, requestedEmail);
            throw new EmailAlreadyExistsException(requestedEmail);
        }

        LogInfo("Email update will require SAGA synchronization", null);
        return (requestedEmail, true);
    }

    /// <summary>
    /// Handle phone update validation
    /// </summary>
    private async Task<(string? newPhone, bool needsSync)> HandlePhoneUpdateAsync(string? requestedPhone, string? originalPhone, bool phoneConfirmed, Guid userId)
    {
        if (string.IsNullOrEmpty(requestedPhone) || requestedPhone == originalPhone)
        {
            return (null, false);
        }

        if (phoneConfirmed)
        {
            LogWarning("Ignoring attempt to update confirmed phone for user: {UserId} - Phone will not be changed", null, userId);
            return (null, false);
        }

        LogInfo("Checking if new phone already exists: {Phone}", null, requestedPhone);

        var phoneExists = await _userRepository.PhoneExistsAsync(requestedPhone);
        if (phoneExists)
        {
            LogWarning("Phone already exists: {Phone}", null, requestedPhone);
            throw new PhoneAlreadyExistsException(requestedPhone);
        }

        LogInfo("Phone update will require SAGA synchronization", null);
        return (requestedPhone, true);
    }

    /// <summary>
    /// Publish SAGA sync event if email or phone update is needed
    /// </summary>
    private async Task PublishSagaSyncEventIfNeededAsync(bool needsEmailSync, bool needsPhoneSync, UserEntity user, string? originalEmail, string? originalPhone, string? newEmail, string? newPhone)
    {
        if (!needsEmailSync && !needsPhoneSync) return;

        var correlationId = Guid.NewGuid().ToString();
        LogInfo("Publishing UserEmailPhoneSyncRequestedEvent with CorrelationId: {CorrelationId}", null, correlationId);

        var syncEvent = new UserEmailPhoneSyncRequestedEvent
        {
            AccountId = user.AccountId,
            UserId = user.Id,
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

    /// <summary>
    /// Apply all field updates to user entity
    /// </summary>
    private bool ApplyFieldUpdates(UserEntity user, UpdateUserRequest request, string? newEmail, string? newPhone)
    {
        var hasChanges = false;

        hasChanges |= UpdateFirstName(user, request.FirstName);
        hasChanges |= UpdateLastName(user, request.LastName);
        hasChanges |= UpdateEmailIfNew(user, newEmail);
        hasChanges |= UpdatePhoneIfNew(user, newPhone);
        hasChanges |= UpdateGender(user, request.Gender);
        hasChanges |= UpdateDateOfBirth(user, request.DateOfBirth);
        hasChanges |= UpdateAddress(user, request.Address);
        hasChanges |= UpdateAvatarUrl(user, request.AvatarUrl);

        return hasChanges;
    }

    private bool UpdateFirstName(UserEntity user, string? firstName)
    {
        if (string.IsNullOrEmpty(firstName) || firstName == user.FirstName) return false;

        if (firstName.Trim().Length < 2)
        {
            LogError(new InvalidOperationException("First name must be at least 2 characters"),
                "First name must be at least 2 characters");
            throw new InvalidOperationException("First name must be at least 2 characters");
        }

        user.FirstName = firstName.Trim();
        return true;
    }

    private bool UpdateLastName(UserEntity user, string? lastName)
    {
        if (string.IsNullOrEmpty(lastName) || lastName == user.LastName) return false;

        if (lastName.Trim().Length < 2)
        {
            LogError(new InvalidOperationException("Last name must be at least 2 characters"),
                "Last name must be at least 2 characters");
            throw new InvalidOperationException("Last name must be at least 2 characters");
        }

        user.LastName = lastName.Trim();
        return true;
    }

    private static bool UpdateEmailIfNew(UserEntity user, string? newEmail)
    {
        if (string.IsNullOrEmpty(newEmail)) return false;

        user.Email = newEmail;
        return true;
    }

    private static bool UpdatePhoneIfNew(UserEntity user, string? newPhone)
    {
        if (string.IsNullOrEmpty(newPhone)) return false;

        user.Phone = newPhone;
        return true;
    }

    private static bool UpdateGender(UserEntity user, Gender? gender)
    {
        if (!gender.HasValue || gender.Value == user.Gender) return false;

        user.Gender = gender.Value;
        return true;
    }

    private bool UpdateDateOfBirth(UserEntity user, DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue || dateOfBirth == user.DateOfBirth) return false;

        // Validate age >= 18
        var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > DateTime.UtcNow.AddYears(-age)) age--;

        if (age < 18)
        {
            LogError(new InvalidOperationException("User must be at least 18 years old"),
                "User must be at least 18 years old");
            throw new InvalidOperationException("User must be at least 18 years old");
        }

        user.DateOfBirth = dateOfBirth;
        return true;
    }

    private bool UpdateAddress(UserEntity user, string? address)
    {
        if (string.IsNullOrEmpty(address) || address == user.Address) return false;

        if (address.Trim().Length < 5)
        {
            LogError(new InvalidOperationException("Address must be at least 5 characters"),
                "Address must be at least 5 characters");
            throw new InvalidOperationException("Address must be at least 5 characters");
        }

        user.Address = address.Trim();
        return true;
    }

    private static bool UpdateAvatarUrl(UserEntity user, string? avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == user.AvatarUrl) return false;

        user.AvatarUrl = avatarUrl;
        return true;
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