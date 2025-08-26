using System.Text.Json.Serialization;

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
        public string? PatientNumber { get; set; }   

        [JsonIgnore] public virtual ICollection<MedicalHistory> MedicalHistory { get; set; } = new List<MedicalHistory>();
        [JsonIgnore] public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
        [JsonIgnore] public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    }
}
