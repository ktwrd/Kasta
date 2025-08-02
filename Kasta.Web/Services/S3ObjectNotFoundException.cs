using Amazon.S3.Model;

namespace Kasta.Web.Services;

public class S3ObjectNotFoundException(GetObjectRequest request, GetObjectResponse response)
    : ApplicationException($"Could not find object {request.Key} in bucket {request.BucketName}")
{
    public GetObjectRequest Request { get; set; } = request;
    public GetObjectResponse Response { get; set; } = response;
}