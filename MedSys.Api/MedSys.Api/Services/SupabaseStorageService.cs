using Supabase;          
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MedSys.Api.Services
{
    public class SupabaseStorageService : IStorageService
    {
        private readonly Supabase.Client _client;   
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

          
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, ct);  
                bytes = ms.ToArray();
            }

            var bucket = _client.Storage.From(_bucket);

            var opts = new Supabase.Storage.FileOptions
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                Upsert = true
            };

          
            await bucket.Upload(bytes, pathInBucket, opts, null, true);

          
            var signed = await bucket.CreateSignedUrl(pathInBucket, 60 * 60 * 24 * 7);
            return signed;


        }
    }
}
