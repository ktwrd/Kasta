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
        var config = new AmazonS3Config()
        {
            ServiceURL = FeatureFlags.S3ServiceUrl,
            ForcePathStyle = FeatureFlags.S3ForcePathStyle
        };
        config.UseHttp = FeatureFlags.S3ServiceUrl.StartsWith("http://");

        _client = new AmazonS3Client(
            FeatureFlags.S3AccessKeyId,
            FeatureFlags.S3AccessSecretKey,
            config);
        _log.Info($"Created S3 Client");
    }
    public async Task<GetObjectResponse> GetObject(string location)
    {
        // Create a GetObject request
        var request = new GetObjectRequest
        {
            BucketName = FeatureFlags.S3BucketName,
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
    public async Task<GetObjectResponse> UploadObject(Stream stream, string location)
    {
        var c = _client;
        var fileTransferUtility = new TransferUtility(c);
        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
        {
            BucketName = FeatureFlags.S3BucketName,
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
        // Create a DeleteObject request
        var request = new DeleteObjectRequest()
        {
            BucketName = FeatureFlags.S3BucketName,
            Key = location
        };
        
        // Issue request and remember to dispose of the response
        var c = _client;
        var response = await c.DeleteObjectAsync(request);
        return response;
        
    }
}