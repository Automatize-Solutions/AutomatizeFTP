using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class FolderView : ReactiveUserControl<IFolderViewModel>
{
    public FolderView()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(FolderButton, true);
        DragDrop.AddDragOverHandler(FolderButton, OnDragOver);
        DragDrop.AddDropHandler(FolderButton, OnDrop);
    }

    private static bool CanMove(FileDragPayload payload, IFolderViewModel target)
    {
        if (payload is null || target?.Provider is null || payload.SourceProvider.Id != target.Provider.Id)
            return false;

        return payload.Items.All(item => !IsSameOrDescendant(item.Path, target.FullPath));
    }

    private static bool IsSameOrDescendant(string sourcePath, string targetPath)
    {
        var source = sourcePath.Replace('\\', '/').TrimEnd('/');
        var target = targetPath.Replace('\\', '/').TrimEnd('/');
        return string.Equals(source, target, StringComparison.OrdinalIgnoreCase) ||
               target.StartsWith(source + "/", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDragOver(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.From(args.DataTransfer);
        var canMove = CanMove(payload, ViewModel);
        args.DragEffects = canMove ? DragDropEffects.Move : DragDropEffects.None;
        args.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.From(args.DataTransfer);
        if (!CanMove(payload, ViewModel))
            return;

        payload = FileDragPayload.Take(args.DataTransfer);
        if (payload is null)
            return;

        try
        {
            foreach (var item in payload.Items)
                await ViewModel.Provider.MoveFileToAsync(item.Path, ViewModel.FullPath, item.Name);

            await ViewModel.Provider.Refresh.Execute().ToTask();
            args.DragEffects = DragDropEffects.Move;
            args.Handled = true;
        }
        catch (Exception exception)
        {
            ViewModel.Provider.ReportError(exception);
        }
    }
}
