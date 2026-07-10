using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.Avalonia.Controls;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Controls;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class ProviderView : ReactiveUserControl<ICloudViewModel>
{
    public ProviderView()
    {
        InitializeComponent();
        FilesList.ContextMenu.Opening += (_, _) =>
        {
            SetContextMenuDataContext(FilesList.ContextMenu, ViewModel);
            CreateFolderMenuItem.Command = ViewModel?.Folder.Open;
            CreateFolderAndEnterMenuItem.Command = ViewModel?.Folder.OpenAndEnter;
        };
        FilesList.FilesDropped += OnFilesDropped;
    }

    private static void SetContextMenuDataContext(ContextMenu contextMenu, ICloudViewModel viewModel)
    {
        contextMenu.DataContext = viewModel;
        foreach (var menuItem in contextMenu.Items.OfType<MenuItem>())
            menuItem.DataContext = viewModel;
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
