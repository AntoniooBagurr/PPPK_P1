using System.Text.RegularExpressions;

namespace OncoWeb.Services;

public class StorageService : IStorageService
{
    private readonly string _root;

    public StorageService(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "storage"); 
        Directory.CreateDirectory(_root);
    }

    public Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.Combine(_root, San(bucket)));
        return Task.CompletedTask;
    }

    public async Task PutAsync(string bucket, string key, Stream data, long length, string contentType, CancellationToken ct = default)
    {
        var full = Path.Combine(_root, San(bucket), key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using var fs = File.Create(full);
        await data.CopyToAsync(fs, ct);
    }

    private static string San(string v) => Regex.Replace(v, @"[^a-zA-Z0-9_\-\.]", "_");
}
