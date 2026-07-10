using System;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.Avalonia.Controls;
using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class ProviderView : ReactiveUserControl<ICloudViewModel>
{
    public ProviderView()
    {
        InitializeComponent();
        FilesList.FilesDropped += OnFilesDropped;
    }

    private async void OnFilesDropped(object sender, FileExplorerDropEventArgs args)
    {
        var payload = args.Payload;
        var target = ViewModel;
        if (target is null || payload.SourceProvider.Id == target.Id)
            return;

        try
        {
            foreach (var item in payload.Items)
                await target.UploadFileFromAsync(item.Path, item.Name, item.IsFolder);

            await target.Refresh.Execute().ToTask();
        }
        catch (Exception exception)
        {
            target.ReportError(exception);
            Console.WriteLine($"Could not upload dropped item: {exception.Message}");
        }
    }
}
