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
    public long Id { get; set; }

    [Required]
    [Column("clinic_id")]
    public long ClinicId { get; set; }

    [Required]
    [Column("exception_date")]
    public DateOnly ExceptionDate { get; set; }

    [StringLength(255)]
    [Column("reason")]
    public string? Reason { get; set; }
}