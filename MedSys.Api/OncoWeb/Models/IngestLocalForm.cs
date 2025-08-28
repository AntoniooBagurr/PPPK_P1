namespace OncoWeb.Models
{
    public class IngestLocalForm
    {
       
        public string Cohort { get; set; } = "";

       
        public IFormFile File { get; set; } = default!;
    }
}
