using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FPTU_ProposalGuard.Application.Configurations;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using Microsoft.Extensions.Options;

namespace FPTU_ProposalGuard.Application.Services;

public class S3Service : IS3Service
{
    private readonly string _bucketName;
    private readonly AmazonS3Client _s3Client;

    public S3Service(IOptionsMonitor<AWSSettings> appSettings)
    {
        var accessKey = appSettings.CurrentValue.AccessKey;
        var secretKey = appSettings.CurrentValue.SecretKey;
        var region = appSettings.CurrentValue.Region;
        _bucketName = appSettings.CurrentValue.BucketName;
        _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
    }


    public async Task UploadFile(Stream file, string fileName, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = file,
            ContentType = contentType,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(request);
    }

    public async Task<string?> GetFileUrl(string fileName)
    {
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = _bucketName,
            Key = fileName,
            Expires = DateTime.Now.AddHours(1)
        };

        var url = await _s3Client.GetPreSignedURLAsync(request);
        return url;
    }

    public async Task<GetObjectResponse?> GetFile(string fileName)
    {
        var request = new GetObjectRequest()
        {
            BucketName = _bucketName,
            Key = fileName,
        };

        var res = await _s3Client.GetObjectAsync(request);
        return res;
    }


    public async Task DeleteFile(string fileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}