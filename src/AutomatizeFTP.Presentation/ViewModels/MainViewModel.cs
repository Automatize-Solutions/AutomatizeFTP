using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AutomatizeFTP.Presentation.ViewModels;

public sealed partial class MainViewModel : ReactiveObject, IMainViewModel
{
    private readonly ReadOnlyObservableCollection<ICloudViewModel> _providers;
    private readonly ObservableAsPropertyHelper<bool> _welcomeScreenCollapsed;
    private readonly ObservableAsPropertyHelper<bool> _welcomeScreenVisible;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<bool> _isReady;
    private readonly ICloudFactory _factory;

    public MainViewModel(
        MainState state,
        ICloudFactory factory,
        CloudViewModelFactory createViewModel,
        IScheduler scheduler,
        ICloudViewModel localProvider = null,
        TransferQueue transferQueue = null)
    {
        _factory = factory;
        LocalProvider = localProvider;
        TransferQueue = transferQueue ?? new TransferQueue();
        LocalProvider?.WhenAnyValue(x => x.CurrentPath)
            .Subscribe(path => state.LocalPath = path);
        Refresh = ReactiveCommand.Create(state.Clouds.Refresh, outputScheduler: scheduler);

        _isLoading = Refresh
            .IsExecuting
            .ToProperty(this, x => x.IsLoading, scheduler: scheduler);

        _isReady = Refresh
            .IsExecuting
            .Select(executing => !executing)
            .ToProperty(this, x => x.IsReady, scheduler: scheduler);

        state
            .Clouds
            .Connect()
            .Filter(cloud => cloud.Type != CloudType.Local)
            .Transform(ps => createViewModel(ps, factory.CreateCloud(ps.Parameters)))
            .Sort(SortExpressionComparer<ICloudViewModel>.Descending(x => x.Created))
            .ObserveOn(scheduler)
            .Bind(out _providers)
            .Subscribe();

        var canRemove = this
            .WhenAnyValue(x => x.SelectedProvider)
            .Select(provider => provider != null);

        Remove = ReactiveCommand.Create(
            () => state.Clouds.RemoveKey(SelectedProvider.Id),
            canRemove,
            outputScheduler: scheduler);

        var canAddProvider = this
            .WhenAnyValue(x => x.SelectedSupportedType)
            .Select(type => SupportedTypes.Contains(type));

        Add = ReactiveCommand.Create(
            () =>
            {
                var cloud = new CloudState { Type = SelectedSupportedType };

                // Pre-select the new provider so it gets picked up by the
                // OnItemAdded subscription below once the pipeline binds it,
                // collapsing the welcome screen instead of silently adding a row.
                state.SelectedProviderId = cloud.Id;
                state.Clouds.AddOrUpdate(cloud);
            },
            canAddProvider,
            outputScheduler: scheduler);

        _welcomeScreenVisible = this
            .WhenAnyValue(x => x.SelectedProvider)
            .Select(provider => provider == null)
            .ToProperty(this, x => x.WelcomeScreenVisible, scheduler: scheduler);

        _welcomeScreenCollapsed = this
            .WhenAnyValue(x => x.WelcomeScreenVisible)
            .Select(visible => !visible)
            .ToProperty(this, x => x.WelcomeScreenCollapsed, scheduler: scheduler);

        var canUnselect = this
            .WhenAnyValue(x => x.SelectedProvider)
            .Select(provider => provider != null);

        Unselect = ReactiveCommand.Create(() => Unit.Default, canUnselect, outputScheduler: scheduler);
        Unselect.Subscribe(unit => SelectedProvider = null);

        var outputCollectionChanges = Clouds
            .ToObservableChangeSet(x => x.Id)
            .Publish()
            .RefCount();

        outputCollectionChanges
            .Filter(provider => provider.Id == state.SelectedProviderId)
            .ObserveOn(scheduler)
            .OnItemAdded(provider => SelectedProvider = provider)
            .Subscribe();

        outputCollectionChanges
            .OnItemRemoved(provider => SelectedProvider = null)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedProvider)
            .Skip(1)
            .Select(provider => provider?.Id ?? Guid.Empty)
            .Subscribe(id => state.SelectedProviderId = id);

        SelectedSupportedType = state.SelectedSupportedType is { } selectedType && SupportedTypes.Contains(selectedType)
            ? selectedType
            : SupportedTypes.First();
        this.WhenAnyValue(x => x.SelectedSupportedType)
            .Subscribe(type => state.SelectedSupportedType = type);

        var localSelection = LocalProvider is null
            ? Observable.Return<IReadOnlyList<IFileViewModel>>(Array.Empty<IFileViewModel>())
            : LocalProvider.WhenAnyValue(x => x.SelectedFiles);
        var remoteSelection = this.WhenAnyValue(x => x.SelectedProvider);

        var canUpload = Observable.CombineLatest(
                remoteSelection,
                localSelection,
                (remote, local) => remote is not null &&
                                   remote.Auth.IsAuthenticated &&
                                   remote.CanInteract &&
                                   local.Count > 0)
            .ObserveOn(scheduler);

        UploadToRemote = ReactiveCommand.CreateFromTask(
            async () =>
            {
                foreach (var source in LocalProvider.SelectedFiles)
                    await SelectedProvider.UploadFileFromAsync(source.Path, source.Name, source.IsFolder).ConfigureAwait(false);

                SelectedProvider.Refresh.Execute().Subscribe();
            },
            canUpload,
            outputScheduler: scheduler);

        var canDownload = Observable.CombineLatest(
                remoteSelection,
                localSelection,
                (remote, local) => remote is not null &&
                                   remote.Auth.IsAuthenticated &&
                                   remote.CanInteract &&
                                   remote.SelectedFiles.Count > 0 &&
                                   local is not null &&
                                   !string.IsNullOrWhiteSpace(LocalProvider.CurrentPath))
            .ObserveOn(scheduler);

        DownloadToLocal = ReactiveCommand.CreateFromTask(
            async () =>
            {
                foreach (var source in SelectedProvider.SelectedFiles)
                    await LocalProvider.DownloadFileToAsync(source.Path, LocalProvider.CurrentPath, source.Name, source.IsFolder).ConfigureAwait(false);

                LocalProvider.Refresh.Execute().Subscribe();
            },
            canDownload,
            outputScheduler: scheduler);
    }

    [Reactive]
    public partial CloudType SelectedSupportedType { get; set; }

    [Reactive]
    public partial ICloudViewModel SelectedProvider { get; set; }

    public ReactiveCommand<Unit, Unit> Unselect { get; }

    public ReactiveCommand<Unit, Unit> Refresh { get; }

    public ReactiveCommand<Unit, Unit> Remove { get; }

    public ReactiveCommand<Unit, Unit> Add { get; }

    public ReactiveCommand<Unit, Unit> UploadToRemote { get; }

    public ReactiveCommand<Unit, Unit> DownloadToLocal { get; }

    public ReadOnlyObservableCollection<ICloudViewModel> Clouds => _providers;

    public ICloudViewModel LocalProvider { get; }

    public TransferQueue TransferQueue { get; }

    public IEnumerable<CloudType> SupportedTypes => _factory.SupportedClouds;

    public bool WelcomeScreenCollapsed => _welcomeScreenCollapsed.Value;

    public bool WelcomeScreenVisible => _welcomeScreenVisible.Value;

    public bool IsLoading => _isLoading.Value;

    public bool IsReady => _isReady.Value;
}
