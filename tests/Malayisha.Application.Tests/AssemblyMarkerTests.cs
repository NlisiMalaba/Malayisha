using Malayisha.Domain;

namespace Malayisha.Application.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void Domain_AssemblyMarker_IsAccessible()
    {
        Assert.NotNull(typeof(AssemblyMarker));
    }
}
