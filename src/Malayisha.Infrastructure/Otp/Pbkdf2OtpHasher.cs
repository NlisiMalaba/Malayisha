using System.Security.Cryptography;
using System.Text;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Options;

namespace Malayisha.Infrastructure.Otp;

internal sealed class Pbkdf2OtpHasher : IOtpHasher
{
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string phoneNumber, string otpCode)
    {
        var salt = CreateSalt(phoneNumber);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            otpCode,
            salt,
            OtpSecurityConstants.Pbkdf2Iterations,
            Algorithm,
            OtpSecurityConstants.Pbkdf2HashSizeBytes);

        return Convert.ToBase64String(hash);
    }

    public bool Verify(string phoneNumber, string otpCode, string otpHash)
    {
        var salt = CreateSalt(phoneNumber);
        var expectedHash = Convert.FromBase64String(otpHash);

        return CryptographicOperations.FixedTimeEquals(
            Rfc2898DeriveBytes.Pbkdf2(
                otpCode,
                salt,
                OtpSecurityConstants.Pbkdf2Iterations,
                Algorithm,
                expectedHash.Length),
            expectedHash);
    }

    private static byte[] CreateSalt(string phoneNumber) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(phoneNumber))[..OtpSecurityConstants.Pbkdf2SaltSizeBytes];
}
