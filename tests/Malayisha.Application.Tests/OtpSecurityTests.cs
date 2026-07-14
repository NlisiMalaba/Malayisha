using Malayisha.Application.Options;
using Malayisha.Infrastructure.Otp;

namespace Malayisha.Application.Tests;

public sealed class Pbkdf2OtpHasherTests
{
    private readonly Pbkdf2OtpHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithSamePhoneAndCode_ReturnsTrue()
    {
        const string phone = "+27123456789";
        const string otp = "123456";

        var hash = _hasher.Hash(phone, otp);

        Assert.True(_hasher.Verify(phone, otp, hash));
    }

    [Fact]
    public void Verify_WithWrongCode_ReturnsFalse()
    {
        const string phone = "+27123456789";
        var hash = _hasher.Hash(phone, "123456");

        Assert.False(_hasher.Verify(phone, "654321", hash));
    }

    [Fact]
    public void Verify_WithDifferentPhone_ReturnsFalse()
    {
        var hash = _hasher.Hash("+27123456789", "123456");

        Assert.False(_hasher.Verify("+27987654321", "123456", hash));
    }

    [Fact]
    public void Hash_UsesPbkdf2WithPhoneDerivedSalt()
    {
        const string phone = "+27123456789";
        const string otp = "123456";

        var firstHash = _hasher.Hash(phone, otp);
        var secondHash = _hasher.Hash(phone, otp);

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(otp, firstHash);
    }
}

public sealed class SecureOtpGeneratorTests
{
    private readonly SecureOtpGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsSixDigitCode()
    {
        var otp = _generator.Generate();

        Assert.Equal(OtpSecurityConstants.OtpCodeLength, otp.Length);
        Assert.True(otp.All(char.IsDigit));
    }

    [Fact]
    public void Generate_ProducesVariableCodes()
    {
        var codes = Enumerable.Range(0, 20).Select(_ => _generator.Generate()).ToHashSet();

        Assert.True(codes.Count > 1);
    }
}
