using System.Text.Json.Serialization;

namespace MedSys.Api.Models
{
    public class MedicalHistory
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string DiseaseName { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [JsonIgnore] public virtual Patient Patient { get; set; } = default!;
    }
}
