namespace OncoWeb.Models
{
    public class IngestResult
    {
        public List<string> Uploaded { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
