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
        Task<List<string>> ListAsync(string bucket, string prefix, CancellationToken ct = default);
        Task<Stream> GetObjectStreamAsync(string bucket, string objectName, CancellationToken ct = default);
    }
}
