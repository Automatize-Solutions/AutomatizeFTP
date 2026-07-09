using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Camelotia.Presentation.Interfaces;
using Camelotia.Services.Interfaces;
using ReactiveUI;

namespace Camelotia.Presentation.ViewModels;

public sealed class AuthViewModel : ReactiveObject, IAuthViewModel
{
    private readonly ObservableAsPropertyHelper<bool> _isAuthenticated;
    private readonly ObservableAsPropertyHelper<bool> _isAnonymous;
    private readonly ICloud _provider;

    public AuthViewModel(IHostAuthViewModel host, ICloud provider, IScheduler scheduler)
    {
        HostAuth = host;
        _provider = provider;

        _isAuthenticated = _provider
            .IsAuthorized
            .DistinctUntilChanged()
            .Log(this, $"Authentication state changed for {provider.Name}")
            .ObserveOn(scheduler)
            .ToProperty(this, x => x.IsAuthenticated, scheduler: scheduler);

        _isAnonymous = this
            .WhenAnyValue(x => x.IsAuthenticated)
            .Select(authenticated => !authenticated)
            .ToProperty(this, x => x.IsAnonymous, scheduler: scheduler);
    }

    public bool IsAnonymous => _isAnonymous.Value;

    public bool IsAuthenticated => _isAuthenticated.Value;

    public bool SupportsHostAuth => _provider.SupportsHostAuth;

    public IHostAuthViewModel HostAuth { get; }
}