using Xunit;

namespace Identity.Functional;

/// <summary>
/// Placeholder that keeps the test project runnable until the real tests exist.
/// </summary>
public class SmokeTest
{
    [Fact(DisplayName = "The functional test project is configured")]
    public void Given_TestProject_When_Running_Then_ShouldExecute()
    {
        Assert.True(true);
    }
}
