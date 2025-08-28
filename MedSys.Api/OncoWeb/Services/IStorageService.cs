namespace OncoWeb.Services
{
    public interface IStorageService
    {
        Task EnsureBucketAsync(string bucket, CancellationToken ct = default);
        Task PutAsync(string bucket, string key, Stream data, long length, string contentType, CancellationToken ct = default);

        Task PutObjectAsync(string bucket, string objectName, Stream data, string contentType, long size, CancellationToken ct)
            => PutAsync(bucket, objectName, data, size, contentType, ct);
    }
}
