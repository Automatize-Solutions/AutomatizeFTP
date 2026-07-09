using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Camelotia.Presentation.AppState;
using Camelotia.Presentation.Interfaces;
using Camelotia.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Camelotia.Presentation.ViewModels;

public delegate IRenameFileViewModel RenameFileViewModelFactory(ICloudViewModel providerViewModel);

public sealed partial class RenameFileViewModel : ReactiveValidationObject, IRenameFileViewModel
{
    private readonly ObservableAsPropertyHelper<bool> _hasErrorMessage;
    private readonly ObservableAsPropertyHelper<string> _errorMessage;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    private readonly ObservableAsPropertyHelper<string> _oldName;

    public RenameFileViewModel(RenameFileState state, ICloudViewModel owner, ICloud provider, IScheduler scheduler)
    {
        _oldName = owner
            .WhenAnyValue(x => x.SelectedFile.Name)
            .ToProperty(this, x => x.OldName, scheduler: scheduler);

        this.ValidationRule(
            x => x.NewName,
            name => !string.IsNullOrWhiteSpace(name),
            "New name shouldn't be empty.");

        var oldRule = this.ValidationRule(
            x => x.OldName,
            name => !string.IsNullOrWhiteSpace(name),
            "Old name shouldn't be empty.");

        Rename = ReactiveCommand.CreateFromTask(
            () => provider.RenameFile(owner.SelectedFile.Path, NewName),
            this.IsValid(),
            outputScheduler: scheduler);

        _isLoading = Rename
            .IsExecuting
            .ToProperty(this, x => x.IsLoading, scheduler: scheduler);

        var canInteract = owner
            .WhenAnyValue(x => x.CanInteract)
            .Skip(1)
            .StartWith(true);

        var canOpen = this
            .WhenAnyValue(x => x.IsVisible)
            .Select(visible => !visible)
            .CombineLatest(
                canInteract,
                oldRule.WhenAnyValue(x => x.IsValid),
                (visible, interact, old) => visible && old && interact);

        var canClose = this
            .WhenAnyValue(x => x.IsVisible)
            .Select(visible => visible);

        Open = ReactiveCommand.Create(() => { }, canOpen, outputScheduler: scheduler);
        Close = ReactiveCommand.Create(() => { }, canClose, outputScheduler: scheduler);

        Open.Select(unit => true)
            .Merge(Close.Select(unit => false))
            .Subscribe(visible => IsVisible = visible);

        Close.Subscribe(x => NewName = string.Empty);
        Rename.InvokeCommand(Close);

        _hasErrorMessage = Rename
            .ThrownExceptions
            .Select(exception => true)
            .Merge(Close.Select(x => false))
            .ToProperty(this, x => x.HasErrorMessage, scheduler: scheduler);

        _errorMessage = Rename
            .ThrownExceptions
            .Select(exception => exception.Message)
            .Log(this, $"Rename file error occured in {provider.Name} for {OldName}")
            .Merge(Close.Select(x => string.Empty))
            .ToProperty(this, x => x.ErrorMessage, scheduler: scheduler);

        NewName = state.NewName;
        this.WhenAnyValue(x => x.NewName)
            .Subscribe(name => state.NewName = name);
    }

    [Reactive]
    public partial bool IsVisible { get; set; }

    [Reactive]
    public partial string NewName { get; set; }

    public bool HasErrorMessage => _hasErrorMessage.Value;

    public string ErrorMessage => _errorMessage.Value;

    public bool IsLoading => _isLoading.Value;

    public string OldName => _oldName.Value;

    public ReactiveCommand<Unit, Unit> Rename { get; }

    public ReactiveCommand<Unit, Unit> Close { get; }

    public ReactiveCommand<Unit, Unit> Open { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _oldName?.Dispose();
            _hasErrorMessage?.Dispose();
            _errorMessage?.Dispose();
            _isLoading?.Dispose();
        }

        base.Dispose(disposing);
    }
}
