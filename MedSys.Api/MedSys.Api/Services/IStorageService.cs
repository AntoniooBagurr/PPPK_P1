namespace MedSys.Api.Services;
public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string contentType, string pathInBucket, CancellationToken ct = default);
}
