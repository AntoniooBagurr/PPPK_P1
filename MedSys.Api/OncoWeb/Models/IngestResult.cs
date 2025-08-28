namespace OncoWeb.Models
{
    public class IngestResult
    {
        public List<IngestItemResult> Downloaded { get; set; } = new();
        public List<string> Uploaded { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class IngestItemResult
    {
        public string Cohort { get; set; } = "";
        public string Url { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public long Bytes { get; set; }
    }
}
