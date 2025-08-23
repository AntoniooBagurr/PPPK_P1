namespace MedSys.Api.Models
{
    public class Medication
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? AtcCode { get; set; }

        public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}
