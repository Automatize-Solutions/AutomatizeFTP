using System.Collections.ObjectModel;
using ReactiveUI;

namespace AutomatizeFTP.Presentation.ViewModels;

public sealed class TransferQueue : ReactiveObject
{
    public ObservableCollection<TransferQueueItem> Active { get; } = new();

    public ObservableCollection<TransferQueueItem> Failed { get; } = new();

    public ObservableCollection<TransferQueueItem> Completed { get; } = new();

    public int ActiveCount => Active.Count;

    public bool HasActiveItems => Active.Count > 0;

    public bool HasNoActiveItems => !HasActiveItems;

    public bool HasFailedItems => Failed.Count > 0;

    public bool HasNoFailedItems => !HasFailedItems;

    public bool HasCompletedItems => Completed.Count > 0;

    public bool HasNoCompletedItems => !HasCompletedItems;

    public bool HasItems => HasActiveItems || HasFailedItems || HasCompletedItems;

    public async Task RunAsync(
        string name,
        string operation,
        string source,
        string destination,
        Func<CancellationToken, IProgress<double>, Task> transfer)
    {
        var item = new TransferQueueItem(name, operation, source, destination);
        Active.Add(item);
        RaiseActiveStateChanged();

        try
        {
            var progress = new Progress<double>(item.ReportProgress);
            await transfer(item.CancellationToken, progress);
            item.CancellationToken.ThrowIfCancellationRequested();
            item.Complete();
            Active.Remove(item);
            Completed.Insert(0, item);
        }
        catch (OperationCanceledException) when (item.IsCancellationRequested)
        {
            item.Cancelled();
            Active.Remove(item);
        }
        catch (Exception exception)
        {
            item.Fail(exception);
            Active.Remove(item);
            Failed.Insert(0, item);
            throw;
        }
        finally
        {
            RaiseActiveStateChanged();
            item.Finish();
            item.Dispose();
        }
    }

    public async Task ClearAsync()
    {
        var activeItems = Active.ToArray();
        foreach (var item in activeItems)
            item.Cancel();

        await Task.WhenAll(activeItems.Select(item => item.Completion));
        Active.Clear();
        Failed.Clear();
        Completed.Clear();
        RaiseActiveStateChanged();
    }

    private void RaiseActiveStateChanged()
    {
        this.RaisePropertyChanged(nameof(ActiveCount));
        this.RaisePropertyChanged(nameof(HasActiveItems));
        this.RaisePropertyChanged(nameof(HasNoActiveItems));
        this.RaisePropertyChanged(nameof(HasFailedItems));
        this.RaisePropertyChanged(nameof(HasNoFailedItems));
        this.RaisePropertyChanged(nameof(HasCompletedItems));
        this.RaisePropertyChanged(nameof(HasNoCompletedItems));
        this.RaisePropertyChanged(nameof(HasItems));
    }
}

public sealed class TransferQueueItem : ReactiveObject, IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string _status;
    private string _errorMessage;
    private double _progress;

    internal TransferQueueItem(string name, string operation, string source, string destination)
    {
        Name = name;
        Operation = operation;
        Source = source;
        Destination = destination;
        _status = "In progress";
    }

    public string Name { get; }

    public string Operation { get; }

    public string Source { get; }

    public string Destination { get; }

    public CancellationToken CancellationToken => _cancellation.Token;

    public bool IsCancellationRequested => _cancellation.IsCancellationRequested;

    public Task Completion => _completion.Task;

    public double Progress
    {
        get => _progress;
        private set => this.RaiseAndSetIfChanged(ref _progress, Math.Clamp(value, 0, 100));
    }

    public string Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public void Dispose() => _cancellation.Dispose();

    internal void Complete() => Status = "Completed";

    internal void ReportProgress(double progress) => Progress = progress;

    internal void Cancel()
    {
        if (!IsCancellationRequested)
            _cancellation.Cancel();
    }

    internal void Cancelled() => Status = "Cancelled";

    internal void Finish() => _completion.TrySetResult();

    internal void Fail(Exception exception)
    {
        Status = "Failed";
        ErrorMessage = exception.Message;
    }
}
