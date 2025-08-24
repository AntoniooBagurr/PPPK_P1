using Supabase;          // samo ovaj using (bez Supabase.Storage)
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MedSys.Api.Services
{
    public class SupabaseStorageService : IStorageService
    {
        private readonly Supabase.Client _client;   // eksplicitno kvalificiran tip
        private readonly string _bucket;

        public SupabaseStorageService(IConfiguration cfg)
        {
            var url = cfg["Supabase:Url"]!;
            var key = cfg["Supabase:ServiceKey"]!;
            _bucket = cfg["Supabase:Bucket"]!;

            _client = new Supabase.Client(url, key, new SupabaseOptions
            {
                AutoRefreshToken = false
            });
        }

        public async Task<string> UploadAsync(
            Stream stream, string contentType, string pathInBucket, CancellationToken ct = default)
        {
            await _client.InitializeAsync();

            // stream -> byte[]
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, ct);  // ovdje i dalje koristiš ct
                bytes = ms.ToArray();
            }

            var bucket = _client.Storage.From(_bucket);

            var opts = new Supabase.Storage.FileOptions
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                Upsert = true
            };

            // ✅ 5. argument je bool (upsert)
            await bucket.Upload(bytes, pathInBucket, opts, null, true);

            // ako je bucket private → potpisani URL (7 dana)
            var signed = await bucket.CreateSignedUrl(pathInBucket, 60 * 60 * 24 * 7);
            return signed;

            // ako je PUBLIC bucket: return bucket.GetPublicUrl(pathInBucket);

        }
    }
}
