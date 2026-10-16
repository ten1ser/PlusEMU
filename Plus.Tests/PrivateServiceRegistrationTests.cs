using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Plus.Tests;

public class PrivateServiceRegistrationTests
{
    [Fact]
    public void AssemblyScanningLeavesPrivateNestedAdaptersForExplicitRegistration()
    {
        var services = new ServiceCollection();
        Program.AddAssignableTo<IRegistrationFixture>(services, typeof(PrivateServiceRegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();
        Assert.IsType<PublicService>(Assert.Single(provider.GetServices<IRegistrationFixture>()));
    }

    public interface IRegistrationFixture { }
    public sealed class PublicService : IRegistrationFixture { }
    private sealed class PrivateAdapter : IRegistrationFixture { }
}
