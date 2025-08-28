using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using OncoWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
var appOptions = builder.Configuration.GetSection("App").Get<AppOptions>() ?? new AppOptions();



// HTTP + servis sloj
builder.Services.AddHttpClient("downloader", c =>
{
    c.Timeout = TimeSpan.FromMinutes(30);
    // neki normalan UA — dosta mirrora to traži
    c.DefaultRequestHeaders.UserAgent.ParseAdd("OncoWeb/1.0 (+https://localhost)");
});
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IngestService>();
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var cfg = new Amazon.S3.AmazonS3Config
    {
        ServiceURL = appOptions.Minio.Endpoint, // npr. "http://localhost:9000"
        ForcePathStyle = true,
        UseHttp = appOptions.Minio.Endpoint.StartsWith("http://"),
        AuthenticationRegion = "us-east-1"
    };
    var creds = new Amazon.Runtime.BasicAWSCredentials(appOptions.Minio.AccessKey, appOptions.Minio.SecretKey);
    return new Amazon.S3.AmazonS3Client(creds, cfg);
});
// Mongo
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(appOptions.Mongo.ConnectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(appOptions.Mongo.Database);
});
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<IMongoDatabase>();
    return db.GetCollection<GeneExpressionDoc>(appOptions.Mongo.Collection);
});

// MinIO preko S3 API-ja
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var cfg = new AmazonS3Config
    {
        ServiceURL = appOptions.Minio.Endpoint,
        ForcePathStyle = true,
        UseHttp = appOptions.Minio.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        AuthenticationRegion = "us-east-1"
    };
    var creds = new BasicAWSCredentials(appOptions.Minio.AccessKey, appOptions.Minio.SecretKey);
    return new AmazonS3Client(creds, cfg);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "OncoWeb API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

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
