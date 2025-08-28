namespace OncoWeb.Models
{
    public class IngestRequest
    {
        public List<IngestJob> Jobs { get; set; } = new();
    }

    public class IngestJob
    {
        public string Cohort { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string? ObjectName { get; set; }
    }
}
