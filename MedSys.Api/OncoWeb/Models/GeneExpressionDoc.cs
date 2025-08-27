using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class GeneExpressionDoc
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("patient_id")] public string PatientId { get; set; } = default!;
    [BsonElement("cancer_cohort")] public string CancerCohort { get; set; } = default!;
    [BsonElement("expressions")] public Dictionary<string, double> Expressions { get; set; } = new();
    [BsonElement("ingested_at_utc")] public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
}
