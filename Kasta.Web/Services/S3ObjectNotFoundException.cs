using Amazon.S3.Model;

namespace Kasta.Web.Services;

public class S3ObjectNotFoundException : ApplicationException
{
    public GetObjectRequest Request { get; set; }
    public GetObjectResponse Response { get; set; }

    public S3ObjectNotFoundException(GetObjectRequest request, GetObjectResponse response)
        : base($"Could not find object {request.Key} in bucket {request.BucketName}")
    {
        Request = request;
        Response = response;
    }
}