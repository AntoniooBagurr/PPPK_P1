using MedSys.Api.Data;
using MedSys.Api.Repositories;
using MedSys.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// EF Core + Npgsql + Lazy
builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
       .UseLazyLoadingProxies());

// Repository factory
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IStorageService, SupabaseStorageService>();

builder.Services.AddScoped<IStorageService, LocalDiskStorageService>();

//builder.Services.AddScoped<IStorageService, SupabaseStorageService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MedSys.Api", Version = "v1" });

    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    c.CustomSchemaIds(t => t.FullName!.Replace('+', '_'));

    c.MapType<DateTimeOffset>(() => new OpenApiSchema { Type = "string", Format = "date-time" });

    c.SupportNonNullableReferenceTypes();
});
builder.Services.AddCors(p => p.AddDefaultPolicy(pb => pb.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();



app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "UNHANDLED for {Path}", ctx.Request.Path);
        throw; 
    }
});

var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.Migrate();
}

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MedSys.Api v1"));
}


app.UseStaticFiles();
app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
