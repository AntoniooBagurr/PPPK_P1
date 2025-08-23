namespace MedSys.Api.Models
{
    public class Patient
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string OIB { get; set; } = default!;
        public DateTime BirthDate { get; set; }
        public string Sex { get; set; } = default!; // 'M' ili 'F'
        public string? PatientNumber { get; set; }   // dodati migracijom kasnije (Ishod 5)

        public virtual ICollection<MedicalHistory> MedicalHistory { get; set; } = new List<MedicalHistory>();
        public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}
