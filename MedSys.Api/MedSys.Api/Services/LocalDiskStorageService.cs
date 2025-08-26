using Microsoft.Extensions.FileProviders;

namespace MedSys.Api.Services;

public class LocalDiskStorageService : IStorageService
{
    private readonly string _root;
    private readonly string _requestPath;

    public LocalDiskStorageService(IWebHostEnvironment env, IConfiguration cfg)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_root);

        _requestPath = "/uploads";
    }

    public async Task<string> UploadAsync(Stream stream, string contentType, string pathInBucket, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, pathInBucket.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            await stream.CopyToAsync(fs, ct);

        return $"{_requestPath}/{pathInBucket.Replace("\\", "/")}";
    }
}
