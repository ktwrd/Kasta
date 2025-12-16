using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Kasta.Shared;
using NLog;
using System.Net;

namespace Kasta.Web.Services;

public class S3Service
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private IAmazonS3 _client;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public S3Service()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        InitializeClient();
    }

    public void InitializeClient()
    {
        var cfg = KastaConfig.Instance;
        if (cfg.LocalFileStorage.Enabled) return;
        if (cfg.S3 == null) throw new InvalidOperationException("S3 not configured!");
        var config = new AmazonS3Config()
        {
            ServiceURL = cfg.S3.ServiceUrl,
            ForcePathStyle = cfg.S3.ForcePathStyle
        };
        config.UseHttp = config.ServiceURL.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase);

        _client = new AmazonS3Client(
            cfg.S3.AccessKey,
            cfg.S3.AccessSecret,
            config);
        _log.Info($"Created S3 Client");
    }
    public async Task<GetObjectResponse?> GetObject(string location)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.S3 == null) throw new InvalidOperationException("S3 not configured!");
        // Create a GetObject request
        var request = new GetObjectRequest
        {
            BucketName = cfg.S3.BucketName,
            Key = location,
        };

        // Issue request and remember to dispose of the response
        var c = _client;
        var response = await c.GetObjectAsync(request);
        if (response.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new S3ObjectNotFoundException(request, response);
        }
        return response;
    }
    public async Task<GetObjectResponse?> UploadObject(Stream stream, string location)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.S3 == null) throw new InvalidOperationException("S3 not configured!");
        var c = _client;
        var fileTransferUtility = new TransferUtility(c);
        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
        {
            BucketName = cfg.S3.BucketName,
            InputStream = stream,
            PartSize = 6291456, // 6 MB.
            Key = location,
            ContentType = MimeTypes.GetMimeType(Path.GetFileName(location)),
            ChecksumAlgorithm = ChecksumAlgorithm.SHA256
        };
        _log.Debug($"[location={location}] Uploading to AWS with {nameof(TransferUtility)}");
        await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);

        return await GetObject(location);
    }

    public async Task<DeleteObjectResponse> DeleteObject(string location)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.S3 == null) throw new InvalidOperationException("S3 not configured!");
        // Create a DeleteObject request
        var request = new DeleteObjectRequest()
        {
            BucketName = cfg.S3.BucketName,
            Key = location
        };
        
        // Issue request and remember to dispose of the response
        var c = _client;
        var response = await c.DeleteObjectAsync(request);
        return response;
    }

    public async Task<string> GeneratePresignedUrl(string location, TimeSpan duration)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.S3 == null) throw new InvalidOperationException("S3 not configured!");
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = cfg.S3.BucketName,
            Key = location,
            Expires = DateTime.UtcNow.AddSeconds(duration.TotalSeconds)
        };
        var response = await _client.GetPreSignedURLAsync(request);
        _log.Trace($"Fetched presigned url {response} for object {location} (expires in {duration})");
        return response;
    }
}