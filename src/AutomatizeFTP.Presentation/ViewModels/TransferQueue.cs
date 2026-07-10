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

    public async Task RunAsync(
        string name,
        string operation,
        string source,
        string destination,
        Func<Task> transfer)
    {
        var item = new TransferQueueItem(name, operation, source, destination);
        Active.Add(item);
        RaiseActiveStateChanged();

        try
        {
            await transfer();
            item.Complete();
            Active.Remove(item);
            Completed.Insert(0, item);
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
        }
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
    }
}

public sealed class TransferQueueItem : ReactiveObject
{
    private string _status;
    private string _errorMessage;

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

    internal void Complete() => Status = "Completed";

    internal void Fail(Exception exception)
    {
        Status = "Failed";
        ErrorMessage = exception.Message;
    }
}
