using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Services;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AutomatizeFTP.Tests.Services;

public sealed class CloudFactoryTests
{
    private readonly MainState _state = new();

    [Fact]
    public void SupportedProviderTypesShouldNotBeEmpty()
    {
        var factory = new CloudFactory();
        factory.SupportedClouds.Should().NotBeEmpty();
        factory.SupportedClouds.Should().Contain(CloudType.Local);
        factory.SupportedClouds.Should().Contain(CloudType.Ftp);
    }

    [Fact]
    public void ShouldInstantiateSupportedProviders()
    {
        var factory = new CloudFactory();
        var provider = factory.CreateCloud(new CloudParameters { Type = CloudType.Local });
        provider.Should().NotBeNull();
        provider.Name.Should().Be(CloudType.Local.ToString());
    }
}
