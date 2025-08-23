using System.ComponentModel.DataAnnotations;
using MedSys.Api.Validation;

namespace MedSys.Api.Dtos;

public class VisitCreateDto
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public DateTimeOffset VisitDateTime { get; set; }

    [Required, VisitType]
    public string VisitType { get; set; } = default!;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
