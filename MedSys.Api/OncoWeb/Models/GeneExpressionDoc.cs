namespace OncoWeb.Models
{
    public class GeneExpressionDoc
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public string PatientId { get; set; } = "";
        public string CancerCohort { get; set; } = "";
        public Dictionary<string, double> Genes { get; set; } = new();
    }
}
