namespace Malayisha.Application.Abstractions.Otp;

public interface IOtpHasher
{
    string Hash(string phoneNumber, string otpCode);

    bool Verify(string phoneNumber, string otpCode, string otpHash);
}
