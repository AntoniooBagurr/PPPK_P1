using System.ComponentModel.DataAnnotations;
using MedSys.Api.Validation;

namespace MedSys.Api.Dtos;

public class PatientDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = default!;
    [Required, MaxLength(100)]
    public string LastName { get; set; } = default!;

    [Required, Oib]
    public string OIB { get; set; } = default!;

    [Required, PastDate]
    public DateTime BirthDate { get; set; }

    [Required, RegularExpression("^(M|F)$", ErrorMessage = "Sex mora biti 'M' ili 'F'.")]
    public string Sex { get; set; } = default!;

    [MaxLength(20)]
    public string? PatientNumber { get; set; } 
}
