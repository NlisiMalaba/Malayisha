using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Malayisha.Application.Abstractions.Storage;
using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure.Storage;

internal sealed partial class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IFileStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> CreatePresignedPutUploadAsync(
        string category,
        Guid ownerId,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateConfiguration();
        ValidateUploadRequest(category, fileName, contentType);

        var objectKey = BuildObjectKey(category, ownerId, fileName);
        var expiresAtUtc = DateTime.UtcNow.AddSeconds(_options.PresignedUrlExpirySeconds);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAtUtc,
            ContentType = contentType
        };

        var uploadUrl = s3Client.GetPreSignedURL(request);
        var cdnUrl = GetCdnUrl(objectKey);

        return Task.FromResult(new PresignedUpload(
            new Uri(uploadUrl),
            objectKey,
            cdnUrl,
            expiresAtUtc));
    }

    public Uri GetCdnUrl(string objectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        var baseUrl = _options.CdnBaseUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "S3 CDN base URL was not configured. Set S3:CdnBaseUrl in application configuration.");
        }

        return new Uri($"{baseUrl}/{objectKey.TrimStart('/')}", UriKind.Absolute);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException(
                "S3 bucket name was not configured. Set S3:BucketName in application configuration.");
        }

        if (string.IsNullOrWhiteSpace(_options.Region))
        {
            throw new InvalidOperationException(
                "S3 region was not configured. Set S3:Region in application configuration.");
        }
    }

    private static void ValidateUploadRequest(string category, string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(category) || category.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("Category must be a single path segment.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException(
                $"Content type '{contentType}' is not allowed. Supported types: {string.Join(", ", AllowedContentTypes)}.",
                nameof(contentType));
        }
    }

    private static string BuildObjectKey(string category, Guid ownerId, string fileName)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        return $"{category}/{ownerId:D}/{Guid.NewGuid():N}-{sanitizedFileName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var nameOnly = Path.GetFileName(fileName);
        var sanitized = InvalidFileNameCharacters().Replace(nameOnly, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "upload" : sanitized.ToLowerInvariant();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._-]")]
    private static partial Regex InvalidFileNameCharacters();
}
