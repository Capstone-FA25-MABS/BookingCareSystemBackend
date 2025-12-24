using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.DTOs;

// Response DTOs
public class UserResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

public class UserListResponse
{
    public List<UserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class UserSearchResponse
{
    public List<UserResponse> Users { get; set; } = new();
    public int TotalFound { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public int Limit { get; set; }
}

// Lightweight DTO for performance-optimized queries (only essential fields)
public class UserBasicInfoResponse
{
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

// DTO for gRPC basic user info (optimized for performance)
public class UserBasicInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}