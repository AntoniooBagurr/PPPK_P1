using System.Text.Json.Serialization;

namespace MedSys.Api.Models
{
    public class PrescriptionItem
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid MedicationId { get; set; }
        public string Dosage { get; set; } = default!;    
        public string Frequency { get; set; } = default!;  
        public int? DurationDays { get; set; }

        [JsonIgnore] public virtual Prescription Prescription { get; set; } = default!;
        [JsonIgnore] public virtual Medication Medication { get; set; } = default!;
    }
}
