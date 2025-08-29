using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OncoWeb.Models
{
    [BsonIgnoreExtraElements]
    public class ClinicalDoc
    {
        [BsonId]
        public ObjectId Id { get; set; }         

        public string PatientId { get; set; } = "";
        public string CancerCohort { get; set; } = "";

        public int? DSS { get; set; }              
        public int? OS { get; set; }          

        public string? ClinicalStage { get; set; }  
    }
}
