namespace Malayisha.Application.Options;

public sealed class AuthOtpOptions
{
    public const string SectionName = "Redis";

    public int OtpTtlSeconds { get; set; } = 300;

    public int LockoutDurationSeconds { get; set; } = 900;

    public int MaxOtpAttempts { get; set; } = 5;
}
