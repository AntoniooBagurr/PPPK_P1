using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MedSys.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

        var options = new DbContextOptionsBuilder<AppDb>()
            .UseNpgsql(cs)
            .UseLazyLoadingProxies()
            .Options;

        return new AppDb(options);
    }
}
