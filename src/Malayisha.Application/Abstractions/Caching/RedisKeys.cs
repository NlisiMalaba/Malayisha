namespace Malayisha.Application.Abstractions.Caching;

public static class RedisKeys
{
    public static string Otp(string phoneNumber) => $"otp:{phoneNumber}";

    public static string OtpLockout(string phoneNumber) => $"otp:lockout:{phoneNumber}";

    public static string OtpAttempts(string phoneNumber) => $"otp:attempts:{phoneNumber}";

    public static string OtpSendRate(string phoneNumber) => $"otp:send:{phoneNumber}";

    public static string RefreshToken(string tokenHash) => $"refresh:{tokenHash}";

    public static string TripSearch(string cacheKey) => $"trip:search:{cacheKey}";

    public static string Session(Guid userId) => $"session:{userId}";
}
