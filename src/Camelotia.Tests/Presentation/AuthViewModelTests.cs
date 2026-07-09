using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Camelotia.Presentation.Interfaces;
using Camelotia.Presentation.ViewModels;
using Camelotia.Services.Interfaces;
using FluentAssertions;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace Camelotia.Tests.Presentation;

public sealed class AuthViewModelTests
{
    private readonly IHostAuthViewModel _host = Substitute.For<IHostAuthViewModel>();
    private readonly ICloud _provider = Substitute.For<ICloud>();

    [Fact]
    public void IsAuthenticatedPropertyShouldDependOnFileProvider()
    {
        var authorized = new Subject<bool>();
        _provider.IsAuthorized.Returns(authorized);

        var model = BuildAuthViewModel();
        model.IsAuthenticated.Should().BeFalse();
        model.IsAnonymous.Should().BeTrue();

        authorized.OnNext(true);
        model.IsAuthenticated.Should().BeTrue();
        model.IsAnonymous.Should().BeFalse();
    }

    [Fact]
    public void SupportsPropsShouldDependOnProvider()
    {
        var model = BuildAuthViewModel();
        model.SupportsHostAuth.Should().BeFalse();

        _provider.SupportsHostAuth.ReturnsForAnyArgs(true);

        model.SupportsHostAuth.Should().BeTrue();
    }

    [Fact]
    public void ShouldReturnInjectedAuthViewModelTypes()
    {
        var model = BuildAuthViewModel();
        model.HostAuth.Should().Be(_host);
    }

    private AuthViewModel BuildAuthViewModel()
    {
        RxApp.MainThreadScheduler = Scheduler.Immediate;
        RxApp.TaskpoolScheduler = Scheduler.Immediate;
        return new AuthViewModel(_host, _provider);
    }
}