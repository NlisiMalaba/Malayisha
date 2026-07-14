using Malayisha.Application.Options;

namespace Malayisha.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    public int DefaultCacheTtlSeconds { get; set; } = 60;

    public int OtpTtlSeconds { get; set; } = 300;

    public int LockoutDurationSeconds { get; set; } = OtpSecurityConstants.DefaultLockoutDurationSeconds;

    public int MaxOtpAttempts { get; set; } = OtpSecurityConstants.DefaultMaxVerifyAttempts;

    public int MaxOtpSendRequests { get; set; } = OtpSecurityConstants.DefaultMaxSendRequests;

    public int OtpSendRateLimitWindowSeconds { get; set; } = OtpSecurityConstants.DefaultSendRateLimitWindowSeconds;
}
