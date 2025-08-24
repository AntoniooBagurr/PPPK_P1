using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace MedSys.Api.Models
{
    public class Visit
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public DateTimeOffset VisitDateTime { get; set; }
        public string VisitType { get; set; } = default!; 
        public string? Notes { get; set; }
        public Guid? DoctorId { get; set; }

        [JsonIgnore] public virtual Patient Patient { get; set; } = default!;
        [JsonIgnore] public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        [JsonIgnore] public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        [JsonIgnore] public virtual Doctor? Doctor { get; set; }
    }
}
