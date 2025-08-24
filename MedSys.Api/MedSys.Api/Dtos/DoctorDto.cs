using System.ComponentModel.DataAnnotations;

namespace MedSys.Api.Dtos;

public class DoctorDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = default!;

    [MaxLength(50)]
    public string? LicenseNo { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }
}
