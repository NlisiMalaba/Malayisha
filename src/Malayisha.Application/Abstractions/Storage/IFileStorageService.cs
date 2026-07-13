namespace Malayisha.Application.Abstractions.Storage;

public sealed record PresignedUpload(
    Uri UploadUrl,
    string ObjectKey,
    Uri CdnUrl,
    DateTime ExpiresAtUtc);

public interface IFileStorageService
{
    Task<PresignedUpload> CreatePresignedPutUploadAsync(
        string category,
        Guid ownerId,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Uri GetCdnUrl(string objectKey);
}
