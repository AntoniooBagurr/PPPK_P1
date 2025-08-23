namespace MedSys.Api.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public Guid VisitId { get; set; }
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long SizeBytes { get; set; }
        public string StorageUrl { get; set; } = default!;
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual Visit Visit { get; set; } = default!;
    }
}
