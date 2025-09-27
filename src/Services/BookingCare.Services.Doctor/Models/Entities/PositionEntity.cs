using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingCare.Services.Doctor.Models.Entities;

[Table("positions")]
public class PositionEntity : BaseEntity
{
    [MaxLength(255)]
    [Column("name")]
    public new string Name { get; set; } = string.Empty;
}

