using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class PrescriptionCreateDto
{
    public Guid VisitId { get; set; }
    [MaxLength(2000)]
    public string? Notes { get; set; }
    public List<PrescriptionItemCreateDto> Items { get; set; } = new();
}
