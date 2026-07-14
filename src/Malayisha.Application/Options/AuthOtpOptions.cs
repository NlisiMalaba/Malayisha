namespace Malayisha.Application.Options;

public sealed class AuthOtpOptions
{
    public const string SectionName = "Redis";

    public int OtpTtlSeconds { get; set; } = 300;

    public int LockoutDurationSeconds { get; set; } = OtpSecurityConstants.DefaultLockoutDurationSeconds;

    public int MaxOtpAttempts { get; set; } = OtpSecurityConstants.DefaultMaxVerifyAttempts;

    public int MaxOtpSendRequests { get; set; } = OtpSecurityConstants.DefaultMaxSendRequests;

    public int OtpSendRateLimitWindowSeconds { get; set; } = OtpSecurityConstants.DefaultSendRateLimitWindowSeconds;
}
