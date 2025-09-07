using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserDto
{
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}
