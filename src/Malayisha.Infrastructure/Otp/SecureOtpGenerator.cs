using System.Security.Cryptography;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Options;

namespace Malayisha.Infrastructure.Otp;

internal sealed class SecureOtpGenerator : IOtpGenerator
{
    private static readonly int UpperBound = (int)Math.Pow(10, OtpSecurityConstants.OtpCodeLength);

    public string Generate()
    {
        var value = RandomNumberGenerator.GetInt32(0, UpperBound);
        return value.ToString($"D{OtpSecurityConstants.OtpCodeLength}");
    }
}
