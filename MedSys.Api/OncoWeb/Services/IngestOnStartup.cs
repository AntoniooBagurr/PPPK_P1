using Microsoft.Extensions.Options;
using OncoWeb;
using OncoWeb.Services;

public class IngestOnStartup : BackgroundService
{
    private readonly IngestService _svc;
    private readonly AppOptions _opt;

    public IngestOnStartup(IngestService svc, IOptions<AppOptions> opt)
    {
        _svc = svc;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      
        await Task.CompletedTask;
    }
}
