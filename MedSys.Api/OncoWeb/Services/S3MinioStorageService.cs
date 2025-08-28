using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace OncoWeb.Services;

public class S3MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;

    public S3MinioStorageService(IAmazonS3 s3) => _s3 = s3;

    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket);
        if (!exists)
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, ct);
        }
    }

    public async Task PutObjectAsync(string bucket, string objectName, Stream data, string contentType, CancellationToken ct = default)
    {
        // pobrini se da stream kreće s početka
        if (data.CanSeek) data.Position = 0;

        var req = new PutObjectRequest
        {
            BucketName = bucket,
            Key = objectName,
            InputStream = data,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };
        await _s3.PutObjectAsync(req, ct);
    }
}
