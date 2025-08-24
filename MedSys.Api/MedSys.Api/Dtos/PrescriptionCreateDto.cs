using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class PrescriptionCreateDto
{
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
