using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace OncoWeb.Services;

public class StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;

    public StorageService(IAmazonS3 s3) => _s3 = s3;

    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket);
        if (!exists)
        {
            var put = new PutBucketRequest { BucketName = bucket, UseClientRegion = true };
            await _s3.PutBucketAsync(put, ct);
        }
    }

    public async Task PutAsync(string bucket, string key, Stream data, long length, string contentType, CancellationToken ct = default)
    {
        if (data.CanSeek) data.Position = 0;

        var req = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = data,
            AutoCloseStream = false,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };
        await _s3.PutObjectAsync(req, ct);
    }
}
