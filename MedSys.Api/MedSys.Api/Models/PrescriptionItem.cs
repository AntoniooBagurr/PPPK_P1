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

        public virtual Prescription Prescription { get; set; } = default!;
        public virtual Medication Medication { get; set; } = default!;
    }
}
