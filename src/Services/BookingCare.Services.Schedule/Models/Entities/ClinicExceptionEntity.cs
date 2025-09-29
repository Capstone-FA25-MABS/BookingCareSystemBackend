using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Schedule.Models.Entities;

/// <summary>
/// Represents clinic/hospital schedule exceptions
/// </summary>
[Table("clinic_exceptions")]
public class ClinicExceptionEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("clinic_id")]
    public Guid ClinicId { get; set; }

    [Required]
    [Column("exception_date")]
    public DateOnly ExceptionDate { get; set; }

    [StringLength(255)]
    [Column("reason")]
    public string? Reason { get; set; }
}