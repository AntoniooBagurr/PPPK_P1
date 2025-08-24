using System.Text.Json.Serialization;
using MedSys.Api.Data;
using MedSys.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MedSys.Api.Services;

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MedSys.Api v1"));
}
app.UseDefaultFiles();  
app.UseStaticFiles();
app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
