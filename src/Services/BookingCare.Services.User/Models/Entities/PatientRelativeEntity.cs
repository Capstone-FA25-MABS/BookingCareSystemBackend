using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.User.Models.Entities;

/// <summary>
/// Entity representing a patient's relative/family member
/// Used for booking appointments on behalf of family members
/// </summary>
[Table("PatientRelatives")]
public class PatientRelativeEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the User who owns this relative profile
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Phone number (optional - relative may not have their own phone)
    /// </summary>
    [MaxLength(10)]
    public string? Phone { get; set; }

    /// <summary>
    /// Relationship to the user (PARENT, CHILD, SPOUSE, etc.)
    /// </summary>
    [Required]
    public Relationship Relationship { get; set; }

    /// <summary>
    /// Health insurance number (Số BHYT) - optional
    /// </summary>
    [MaxLength(20)]
    public string? HealthInsuranceNumber { get; set; }

    /// <summary>
    /// Identity card number (CMND/CCCD) - optional
    /// </summary>
    [MaxLength(20)]
    public string? IdentityNumber { get; set; }

    /// <summary>
    /// Additional notes about the relative (allergies, medical conditions, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    /// <summary>
    /// Get full name of the relative
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
