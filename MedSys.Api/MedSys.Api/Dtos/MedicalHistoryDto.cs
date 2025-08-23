using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class MedicalHistoryDto : IValidatableObject
{
    [Required]
    public Guid PatientId { get; set; }

    [Required, MaxLength(200)]
    public string DiseaseName { get; set; } = default!;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
            yield return new ValidationResult("EndDate ne smije biti prije StartDate.", new[] { nameof(EndDate) });
    }
}
