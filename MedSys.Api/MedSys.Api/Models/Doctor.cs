using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MedSys.Api.Models;

public class Doctor
{
    public Guid Id { get; set; }
    [Required]
    public string FullName { get; set; } = default!;
    public string? LicenseNo { get; set; }
    [Required]
    public string? Email { get; set; }
    public string? Phone { get; set; }

    [JsonIgnore] public string PwdHash { get; set; } = default!;
    [JsonIgnore] public string PwdSalt { get; set; } = default!;
    [JsonIgnore] public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
