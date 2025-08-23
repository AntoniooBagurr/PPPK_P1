using System.Reflection.Metadata;

namespace MedSys.Api.Models
{
    public class Visit
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public DateTimeOffset VisitDateTime { get; set; }
        public string VisitType { get; set; } = default!; // GP, KRV, ...
        public string? Notes { get; set; }

        public virtual Patient Patient { get; set; } = default!;
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
