using Malayisha.Application.Validation;

namespace Malayisha.Application.Tests;

public sealed class InputSanitizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  hello  ", "hello")]
    [InlineData("Tom & Jerry", "Tom & Jerry")]
    [InlineData("value\u0000hidden", "valuehidden")]
    public void Sanitize_NormalizesStringInput(string? input, string expected) =>
        Assert.Equal(expected, InputSanitizer.Sanitize(input));

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        var input = "<script>alert('x')</script>Johannesburg";

        var sanitized = InputSanitizer.Sanitize(input);

        Assert.Equal("Johannesburg", sanitized);
    }

    [Fact]
    public void SanitizeInstance_SanitizesRecordStringProperties()
    {
        var command = new TestSanitizationCommand(
            Guid.NewGuid(),
            "  <script>alert(1)</script>Alice  ",
            ["  Route A  ", "<script>x</script>Route B"]);

        InputSanitizer.SanitizeInstance(command);

        Assert.Equal("Alice", command.DisplayName);
        Assert.Equal(["Route A", "Route B"], command.RoutesServed);
    }

    private sealed record TestSanitizationCommand(
        Guid UserId,
        string DisplayName,
        IReadOnlyList<string> RoutesServed);
}
