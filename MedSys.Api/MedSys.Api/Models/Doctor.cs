using System.Text.Json.Serialization;

namespace MedSys.Api.Models;

public class Doctor
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? LicenseNo { get; set; }   
    public string? Email { get; set; }
    public string? Phone { get; set; }

    [JsonIgnore] public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
