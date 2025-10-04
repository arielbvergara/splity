using Amazon.S3;
using Amazon.S3.Transfer;

namespace Splity.Shared.Storage;

public class S3BucketService(IAmazonS3? s3Client, string bucketName, string bucketRegion) : IS3BucketService
{
    public async Task<string> UploadFileAsync(byte[] fileContent, string fileName, string? keyPrefix = null)
    {
        if (fileContent == null || fileContent.Length == 0)
            throw new ArgumentException("File content cannot be null or empty", nameof(fileContent));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        var key = $"{keyPrefix ?? string.Empty}_{Guid.NewGuid()}_{fileName}";

        using var stream = new MemoryStream(fileContent);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = key,
            BucketName = bucketName,
        };

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        // If the bucket policy allows public read, return direct URL:
        return $"https://{bucketName}.s3.{bucketRegion}.amazonaws.com/{key}";
    }

    // public string GetPresignedUrl(string key, int expireMinutes = 60)
    // {
    //     var request = new GetPreSignedUrlRequest
    //     {
    //         BucketName = bucketName,
    //         Key = key,
    //         Expires = DateTime.UtcNow.AddMinutes(expireMinutes)
    //     };
    //     return s3Client.GetPreSignedURL(request);
    // }
}