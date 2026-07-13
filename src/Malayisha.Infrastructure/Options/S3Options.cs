namespace Malayisha.Infrastructure.Options;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "af-south-1";

    public string CdnBaseUrl { get; set; } = string.Empty;

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public int PresignedUrlExpirySeconds { get; set; } = 900;

    public long MaxUploadSizeBytes { get; set; } = 5_242_880;
}
