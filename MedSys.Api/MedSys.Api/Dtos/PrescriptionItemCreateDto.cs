using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class PrescriptionItemCreateDto
{
    [Required] public Guid MedicationId { get; set; }
    [Required, MaxLength(100)] public string Dosage { get; set; } = default!;
    [Required, MaxLength(100)] public string Frequency { get; set; } = default!;
    public int? DurationDays { get; set; }
}
