using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Extensions;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AutomatizeFTP.Presentation.ViewModels;

public delegate ICloudViewModel CloudViewModelFactory(CloudState state, ICloud provider);

public sealed partial class CloudViewModel : ReactiveObject, ICloudViewModel, IActivatableViewModel
{
    private readonly ObservableAsPropertyHelper<IEnumerable<IFolderViewModel>> _breadCrumbs;
    private readonly ObservableAsPropertyHelper<IEnumerable<IFileViewModel>> _files;
    private readonly ObservableAsPropertyHelper<bool> _isCurrentPathEmpty;
    private readonly ObservableAsPropertyHelper<bool> _showBreadCrumbs;
    private readonly ObservableAsPropertyHelper<bool> _hasErrorMessage;
    private readonly ObservableAsPropertyHelper<bool> _hideBreadCrumbs;
    private readonly ObservableAsPropertyHelper<string> _currentPath;
    private readonly ObservableAsPropertyHelper<bool> _canInteract;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<bool> _canLogout;
    private readonly ObservableAsPropertyHelper<bool> _isReady;
    private readonly IScheduler _scheduler;
    private readonly ICloud _cloud;

    public CloudViewModel(
        CloudState state,
        CreateFolderViewModelFactory createFolderFactory,
        RenameFileViewModelFactory renameFactory,
        FileViewModelFactory fileFactory,
        FolderViewModelFactory folderFactory,
        IAuthViewModel auth,
        IFileManager files,
        ICloud cloud,
        IScheduler scheduler)
    {
        _scheduler = scheduler;
        _cloud = cloud;
        Folder = createFolderFactory(this);
        Rename = renameFactory(this);
        Auth = auth;

        var canInteract = this
            .WhenAnyValue(
                x => x.Folder.IsVisible,
                x => x.Rename.IsVisible,
                (folder, rename) => !folder && !rename);

        _canInteract = canInteract
            .ToProperty(this, x => x.CanInteract, scheduler: _scheduler);

        var canRefresh = this
            .WhenAnyValue(
                x => x.Folder.IsVisible,
                x => x.Rename.IsVisible,
                x => x.Auth.IsAuthenticated,
                (folder, rename, authenticated) => !folder && !rename && authenticated);

        Refresh = ReactiveCommand.CreateFromTask(
            () => cloud.GetFiles(CurrentPath),
            canRefresh,
            outputScheduler: _scheduler);

        _files = Refresh
            .Select(
                items => items
                    .Select(file => fileFactory(file, this))
                    .OrderByDescending(file => file.IsFolder)
                    .ThenBy(file => file.Name)
                    .ToList())
            .Where(items => Files == null || !items.SequenceEqual(Files))
            .ToProperty(this, x => x.Files, scheduler: _scheduler);

        _isLoading = Refresh
            .IsExecuting
            .ToProperty(this, x => x.IsLoading, scheduler: _scheduler);

        _isReady = Refresh
            .IsExecuting
            .Skip(1)
            .Select(executing => !executing)
            .ToProperty(this, x => x.IsReady, scheduler: _scheduler);

        var canOpenCurrentPath = this
            .WhenAnyValue(x => x.SelectedFile)
            .Select(file => file != null && file.IsFolder)
            .CombineLatest(Refresh.IsExecuting, canInteract, (folder, busy, ci) => folder && ci && !busy);

        Open = ReactiveCommand.Create(
            () => Path.Combine(CurrentPath, SelectedFile.Name),
            canOpenCurrentPath,
            outputScheduler: _scheduler);

        // Back and CurrentPath are mutually dependent: this pipeline gates Back,
        // while the CurrentPath helper below is fed by Back. The helper therefore
        // does not exist yet, and CurrentPath still reads null, which Where would
        // swallow -- leaving CombineLatest without a first value and Back disabled
        // forever. Seed the pipeline with the very value the helper starts from.
        var initialPath = state.CurrentPath ?? cloud.InitialPath;

        var canCurrentPathGoBack = this
            .WhenAnyValue(x => x.CurrentPath)
            .StartWith(initialPath)
            .Where(path => path != null)
            .Select(path => path.Length > cloud.InitialPath.Length)
            .CombineLatest(Refresh.IsExecuting, canInteract, (valid, busy, ci) => valid && ci && !busy);

        Back = ReactiveCommand.Create(
            () => Path.GetDirectoryName(CurrentPath),
            canCurrentPathGoBack,
            outputScheduler: _scheduler);

        SetPath = ReactiveCommand.Create<string, string>(path => path, outputScheduler: _scheduler);

        _currentPath = Open
            .Merge(Back)
            .Merge(SetPath)
            .Select(path => path ?? cloud.InitialPath)
            .DistinctUntilChanged()
            .Log(this, $"Current path changed in {cloud.Name}")
            .ToProperty(this, x => x.CurrentPath, initialPath, scheduler: _scheduler);

        var getBreadCrumbs = ReactiveCommand.CreateFromTask(
            () => cloud.GetBreadCrumbs(CurrentPath),
            outputScheduler: _scheduler);

        _breadCrumbs = getBreadCrumbs
            .Where(items => items != null && items.Any())
            .Select(items => items.Select(folder => folderFactory(folder, this)))
            .ToProperty(this, x => x.BreadCrumbs, scheduler: _scheduler);

        _showBreadCrumbs = getBreadCrumbs
            .ThrownExceptions
            .Select(exception => false)
            .Merge(getBreadCrumbs.Select(items => items != null && items.Any()))
            .ObserveOn(_scheduler)
            .ToProperty(this, x => x.ShowBreadCrumbs, scheduler: _scheduler);

        _hideBreadCrumbs = this
            .WhenAnyValue(x => x.ShowBreadCrumbs)
            .Select(show => !show)
            .ToProperty(this, x => x.HideBreadCrumbs, scheduler: _scheduler);

        this.WhenAnyValue(x => x.CurrentPath, x => x.IsReady)
            .Where(x => x.Item1 != null && x.Item2)
            .Select(_ => Unit.Default)
            .InvokeCommand(getBreadCrumbs);

        this.WhenAnyValue(x => x.CurrentPath)
            .Skip(1)
            .Select(_ => Unit.Default)
            .InvokeCommand(Refresh);

        this.WhenAnyValue(x => x.CurrentPath)
            .Subscribe(_ => SelectedFile = null);

        _isCurrentPathEmpty = this
            .WhenAnyValue(x => x.Files)
            .Skip(1)
            .Where(items => items != null)
            .Select(items => !items.Any())
            .ToProperty(this, x => x.IsCurrentPathEmpty, scheduler: _scheduler);

        _hasErrorMessage = Refresh
            .ThrownExceptions
            .Select(exception => true)
            .ObserveOn(_scheduler)
            .Merge(Refresh.Select(x => false))
            .ToProperty(this, x => x.HasErrorMessage, scheduler: _scheduler);

        var canUploadToCurrentPath = this
            .WhenAnyValue(x => x.CurrentPath)
            .Select(path => path != null)
            .CombineLatest(Refresh.IsExecuting, canInteract, (up, loading, can) => up && can && !loading);

        UploadToCurrentPath = ReactiveCommand.CreateFromObservable(
            () => Observable
                .FromAsync(files.OpenRead)
                .Where(response => response.Name != null && response.Stream != null)
                .Select(args => _cloud.UploadFile(CurrentPath, args.Stream, args.Name))
                .SelectMany(task => task.ToObservable()),
            canUploadToCurrentPath,
            outputScheduler: _scheduler);

        UploadToCurrentPath.InvokeCommand(Refresh);

        var canDownloadSelectedFile = this
            .WhenAnyValue(x => x.SelectedFile)
            .Select(file => file?.IsFolder == false)
            .CombineLatest(Refresh.IsExecuting, canInteract, (down, loading, can) => down && !loading && can);

        DownloadSelectedFile = ReactiveCommand.CreateFromObservable(
            () => Observable
                .FromAsync(() => files.OpenWrite(SelectedFile.Name))
                .Where(stream => stream != null)
                .Select(stream => _cloud.DownloadFile(SelectedFile.Path, stream))
                .SelectMany(task => task.ToObservable()),
            canDownloadSelectedFile,
            outputScheduler: _scheduler);

        var canLogout = cloud
            .IsAuthorized
            .DistinctUntilChanged()
            .Select(loggedIn => loggedIn && cloud.SupportsHostAuth)
            .CombineLatest(canInteract, (logout, interact) => logout && interact)
            .ObserveOn(_scheduler);

        Logout = ReactiveCommand.CreateFromTask(cloud.Logout, canLogout, outputScheduler: _scheduler);

        _canLogout = canLogout
            .ToProperty(this, x => x.CanLogout, scheduler: _scheduler);

        var canDeleteSelection = this
            .WhenAnyValue(x => x.SelectedFile)
            .Select(file => file?.IsFolder == false)
            .CombineLatest(Refresh.IsExecuting, canInteract, (del, loading, ci) => del && !loading && ci);

        DeleteSelectedFile = ReactiveCommand.CreateFromTask(
            () => cloud.Delete(SelectedFile.Path, SelectedFile.IsFolder),
            canDeleteSelection,
            outputScheduler: _scheduler);

        DeleteSelectedFile.InvokeCommand(Refresh);

        var canUnselectFile = this
            .WhenAnyValue(x => x.SelectedFile)
            .Select(selection => selection != null)
            .CombineLatest(Refresh.IsExecuting, canInteract, (sel, loading, ci) => sel && !loading && ci);

        UnselectFile = ReactiveCommand.Create(
            () => { SelectedFile = null; },
            canUnselectFile,
            outputScheduler: _scheduler);

        UploadToCurrentPath.ThrownExceptions
            .Merge(DeleteSelectedFile.ThrownExceptions)
            .Merge(DownloadSelectedFile.ThrownExceptions)
            .Merge(Refresh.ThrownExceptions)
            .Merge(getBreadCrumbs.ThrownExceptions)
            .Log(this, $"Exception occured in provider {cloud.Name}")
            .Subscribe();

        this.WhenAnyValue(x => x.CurrentPath)
            .Subscribe(path => state.CurrentPath = path);

        this.WhenAnyValue(x => x.Auth.IsAuthenticated)
            .Select(authenticated => authenticated ? _cloud.Parameters?.Token : null)
            .Subscribe(token => state.Token = token);

        this.WhenAnyValue(x => x.Auth.IsAuthenticated)
            .Select(authenticated => authenticated ? _cloud.Parameters?.User : null)
            .Subscribe(user => state.User = user);

        this.WhenActivated(ActivateAutoRefresh);
    }

    [Reactive]
    public partial int RefreshingIn { get; private set; }

    [Reactive]
    public partial IFileViewModel SelectedFile { get; set; }

    public bool IsCurrentPathEmpty => _isCurrentPathEmpty.Value;

    public IEnumerable<IFileViewModel> Files => _files.Value;

    public IEnumerable<IFolderViewModel> BreadCrumbs => _breadCrumbs.Value;

    public bool ShowBreadCrumbs => _showBreadCrumbs.Value;

    public bool HideBreadCrumbs => _hideBreadCrumbs.Value;

    public string CurrentPath => _currentPath?.Value;

    public bool CanLogout => _canLogout.Value;

    public bool IsLoading => _isLoading.Value;

    public bool HasErrorMessage => _hasErrorMessage.Value;

    public bool IsReady => _isReady.Value;

    public bool CanInteract => _canInteract?.Value ?? false;

    public IAuthViewModel Auth { get; }

    public IRenameFileViewModel Rename { get; }

    public ICreateFolderViewModel Folder { get; }

    public ViewModelActivator Activator { get; } = new();

    public Guid Id => _cloud.Id;

    public string Name => _cloud.Name;

    public DateTime Created => _cloud.Created;

    public string Size => _cloud.Size?.ByteSizeToString() ?? "Unknown";

    public string Description => $"{_cloud.Name} file system.";

    public ReactiveCommand<Unit, Unit> DownloadSelectedFile { get; }

    public ReactiveCommand<Unit, Unit> UploadToCurrentPath { get; }

    public ReactiveCommand<Unit, Unit> DeleteSelectedFile { get; }

    public ReactiveCommand<Unit, Unit> UnselectFile { get; }

    public ReactiveCommand<Unit, IEnumerable<FileModel>> Refresh { get; }

    public ReactiveCommand<Unit, Unit> Logout { get; }

    public ReactiveCommand<Unit, string> Back { get; }

    public ReactiveCommand<Unit, string> Open { get; }

    public ReactiveCommand<string, string> SetPath { get; }

    private void ActivateAutoRefresh(CompositeDisposable disposable)
    {
        this.WhenAnyValue(x => x.Auth.IsAuthenticated)
            .Where(authenticated => authenticated)
            .Select(_ => Unit.Default)
            .InvokeCommand(Refresh)
            .DisposeWith(disposable);

        Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            .Select(_ => RefreshingIn - 1)
            .Where(value => value >= 0)
            .ObserveOn(_scheduler)
            .Subscribe(x => RefreshingIn = x)
            .DisposeWith(disposable);

        this.WhenAnyValue(x => x.RefreshingIn)
            .Skip(1)
            .Where(refreshing => refreshing == 0)
            .Log(this, $"Refreshing provider {_cloud.Name} path {CurrentPath}")
            .Select(_ => Unit.Default)
            .InvokeCommand(Refresh)
            .DisposeWith(disposable);

        Refresh.Select(_ => 30)
            .StartWith(30)
            .ObserveOn(_scheduler)
            .Subscribe(x => RefreshingIn = x)
            .DisposeWith(disposable);

        this.WhenAnyValue(x => x.CanInteract)
            .Skip(1)
            .Where(interact => interact)
            .Select(x => Unit.Default)
            .InvokeCommand(Refresh)
            .DisposeWith(disposable);
    }
}
