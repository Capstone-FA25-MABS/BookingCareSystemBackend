using AutoMapper;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using BookingCare.Services.User.Repositories;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.User.Services;

public class UserService : BaseService, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger) : base(logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
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
    
    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest updateUserRequest)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Updating user with ID: {UserId}", null, id);
            ValidateGuid(id, nameof(id));
            ValidateRequired(updateUserRequest, nameof(updateUserRequest));

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                throw new UserNotFoundException(id);
            }

            _mapper.Map(updateUserRequest, existingUser);
            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            
            LogInfo("User updated successfully with ID: {UserId}", null, id);
            return _mapper.Map<UserResponse>(updatedUser);
            
        }, "UpdateUser");
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
    
}