using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class PrescriptionItemCreateDto
{
    public string MedicationName { get; set; } = default!;
    public string Dosage { get; set; } = default!;
    public string Frequency { get; set; } = default!;
    public int? DurationDays { get; set; }
}
