namespace OncoWeb;

public class AppOptions
{
    public string[] Genes { get; set; } =
    [
        "C6orf150","CCL5","CXCL10","TMEM173","CXCL9","CXCL11",
        "NFKB1","IKBKE","IRF3","TREX1","ATM","IL6","IL8"
    ];

    public MongoOptions Mongo { get; set; } = new();
    public MinioOptions Minio { get; set; } = new();

    public bool IngestOnStartup { get; set; } = false;
}

public class MongoOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "tcga";
    public string Collection { get; set; } = "gene_expression";
}

public class MinioOptions
{
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "tcga";
}
