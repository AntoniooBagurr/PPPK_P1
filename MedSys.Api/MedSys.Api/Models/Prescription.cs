using System.Text.Json.Serialization;

namespace MedSys.Api.Models;

public class Prescription
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }

    [JsonIgnore] public virtual Visit Visit { get; set; } = default!;
    [JsonIgnore] public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
