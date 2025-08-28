namespace OncoWeb.Services
{
    public interface IStorageService
    {
        Task EnsureBucketAsync(string bucket, CancellationToken ct = default);

        Task PutObjectAsync(
            string bucket,
            string objectName,
            Stream data,
            string contentType,
            CancellationToken ct = default);
    }
}
