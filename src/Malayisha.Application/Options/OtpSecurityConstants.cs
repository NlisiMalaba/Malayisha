namespace Malayisha.Application.Options;

public static class OtpSecurityConstants
{
    public const int Pbkdf2Iterations = 100_000;

    public const int Pbkdf2SaltSizeBytes = 16;

    public const int Pbkdf2HashSizeBytes = 32;

    public const int OtpCodeLength = 6;

    public const int DefaultMaxVerifyAttempts = 5;

    public const int DefaultLockoutDurationSeconds = 900;

    public const int DefaultMaxSendRequests = 5;

    public const int DefaultSendRateLimitWindowSeconds = 900;
}
