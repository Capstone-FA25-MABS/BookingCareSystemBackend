using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingCare.Services.Hospital.Enums;

namespace BookingCare.Services.Hospital.Models.Entities;

[Table("hospital_registrations")]
public class HospitalRegistrationEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("hospital_name")]
    public string HospitalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Column("license_file")]
    public string LicenseFile { get; set; } = string.Empty;

    [Required]
    [Column("business_certificate_file")]
    public string BusinessCertificateFile { get; set; } = string.Empty;

    [Required]
    [Column("identity_card_file")]
    public string IdentityCardFile { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("tax_code")]
    public string TaxCode { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public RegistrationStatus Status { get; set; } = RegistrationStatus.PENDING;

    [Column("contract_file")]
    public string? ContractFile { get; set; }

    [Column("hospital_id")]
    public Guid? HospitalId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation property (optional, if the hospital is created)
    public virtual HospitalEntity? Hospital { get; set; }
}

