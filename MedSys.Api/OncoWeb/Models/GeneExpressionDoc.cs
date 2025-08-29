using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OncoWeb.Models
{
    public class GeneExpressionDoc
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string PatientId { get; set; } = "";
        public string CancerCohort { get; set; } = "";
        public Dictionary<string, double> Genes { get; set; } = new();
    }
}
