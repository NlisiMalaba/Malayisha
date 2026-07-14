using System.Security.Cryptography;
using Malayisha.Application.Abstractions.Otp;

namespace Malayisha.Infrastructure.Otp;

internal sealed class SecureOtpGenerator : IOtpGenerator
{
    public string Generate()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }
}
