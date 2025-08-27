namespace OncoWeb.Models
{
    public class IngestRequest
    {
        public string Cohort { get; set; } = string.Empty;

        public List<string> TsvUrls { get; set; } = new();
    }
}
