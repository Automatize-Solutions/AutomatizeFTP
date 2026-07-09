using Camelotia.Presentation.AppState;
using Camelotia.Services;
using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Camelotia.Tests.Services;

public sealed class CloudFactoryTests
{
    private readonly IAuthenticator _authenticator = Substitute.For<IAuthenticator>();
    private readonly MainState _state = new();

    [Fact]
    public void SupportedProviderTypesShouldNotBeEmpty()
    {
        var factory = new CloudFactory(_state.CloudConfiguration, _authenticator);
        factory.SupportedClouds.Should().NotBeEmpty();
        factory.SupportedClouds.Should().Contain(CloudType.Local);
        factory.SupportedClouds.Should().Contain(CloudType.Ftp);
    }

    [Fact]
    public void ShouldInstantiateSupportedProviders()
    {
        var factory = new CloudFactory(_state.CloudConfiguration, _authenticator);
        var provider = factory.CreateCloud(new CloudParameters { Type = CloudType.Local });
        provider.Should().NotBeNull();
        provider.Name.Should().Be(CloudType.Local.ToString());
    }
}
