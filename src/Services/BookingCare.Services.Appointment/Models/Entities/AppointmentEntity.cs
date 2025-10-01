using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Appointment.Enums;

namespace BookingCare.Services.Appointment.Models.Entities;

[Table("Appointments")]
public class AppointmentEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PatientId { get; set; }

    public Guid? DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public Guid AppointmentTimeId { get; set; }

    public Guid? HospitalId { get; set; }

    [Required]
    public AppointmentType AppointmentType { get; set; } = AppointmentType.IN_PERSON;

    [Required]
    [MaxLength(20)]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.CONFIRMED;

    [MaxLength(4000)]
    public string? Reason { get; set; }

    [MaxLength(4000)]
    public string? Result { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
