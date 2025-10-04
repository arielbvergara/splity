namespace Splity.Shared.Storage;

public interface IS3BucketService
{
    Task<string> UploadFileAsync(byte[] fileContent, string fileName, string? keyPrefix = null);
}