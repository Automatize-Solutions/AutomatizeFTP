using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class LocalFilesView : ReactiveUserControl<ICloudViewModel>
{
    private string _searchBuffer = string.Empty;
    private DateTime _lastSearchAt = DateTime.MinValue;
    private IFileViewModel _selectionAnchor;

    public LocalFilesView()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        DragDrop.AddDragOverHandler(this, OnDragOver);
        DragDrop.AddDropHandler(this, OnDrop);
        DragDrop.SetAllowDrop(FilesList, true);
        DragDrop.AddDragOverHandler(FilesList, OnDragOver);
        DragDrop.AddDropHandler(FilesList, OnDrop);
    }

    private static ICloudViewModel GetTargetViewModel(object sender) => sender switch
    {
        LocalFilesView view => view.ViewModel,
        ListBox list => list.DataContext as ICloudViewModel,
        _ => null
    };

    private static string GetSearchCharacter(Key key)
    {
        var value = (int)key;
        var letterStart = (int)Key.A;
        var letterEnd = (int)Key.Z;
        if (value >= letterStart && value <= letterEnd)
            return ((char)('a' + value - letterStart)).ToString();

        var digitStart = (int)Key.D0;
        var digitEnd = (int)Key.D9;
        if (value >= digitStart && value <= digitEnd)
            return ((char)('0' + value - digitStart)).ToString();

        return null;
    }

    private static IFileViewModel FindFileViewModel(object source)
    {
        for (var control = source as Control; control is not null; control = control.Parent as Control)
        {
            if (control.DataContext is IFileViewModel file)
                return file;
        }

        return null;
    }

    private static void OnDragOver(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.From(args.DataTransfer);
        var target = GetTargetViewModel(sender);
        args.DragEffects = payload is not null &&
                           target is not null &&
                           payload.SourceProvider.Id != target.Id
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        args.Handled = true;
    }

    private void OnFilesPointerPressed(object sender, PointerPressedEventArgs args)
    {
        if (sender is ListBox list)
        {
            list.Focus();
            _selectionAnchor = args.GetCurrentPoint(list).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed
                ? FindFileViewModel(args.Source)
                : null;
        }
    }

    private void OnFilesSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        ViewModel?.SetSelectedFiles(FilesList.SelectedItems.OfType<IFileViewModel>());
    }

    private void OnFilesPointerMoved(object sender, PointerEventArgs args)
    {
        if (_selectionAnchor is null || args.GetCurrentPoint(FilesList).Properties.IsLeftButtonPressed == false)
            return;

        var target = FindFileViewModel(FilesList.InputHitTest(args.GetPosition(FilesList)));
        if (target is null)
            return;

        var files = FilesList.Items.OfType<IFileViewModel>().ToList();
        var anchorIndex = files.IndexOf(_selectionAnchor);
        var targetIndex = files.IndexOf(target);
        if (anchorIndex < 0 || targetIndex < 0)
            return;

        var start = Math.Min(anchorIndex, targetIndex);
        var count = Math.Abs(anchorIndex - targetIndex) + 1;
        FilesList.SelectedItems = files.GetRange(start, count);
        args.Handled = true;
    }

    private void OnFilesPointerReleased(object sender, PointerReleasedEventArgs args)
    {
        _selectionAnchor = null;
    }

    private async void OnDrop(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.Take(args.DataTransfer);
        var target = GetTargetViewModel(sender);
        if (payload is null || target is null || payload.SourceProvider.Id == target.Id)
            return;

        args.DragEffects = DragDropEffects.Copy;
        args.Handled = true;

        try
        {
            await payload.SourceProvider.DownloadFileToAsync(
                payload.Path,
                target.CurrentPath,
                payload.Name,
                payload.IsFolder);
            await target.Refresh.Execute().ToTask();
        }
        catch (Exception exception)
        {
            target.ReportError(exception);
            Console.WriteLine($"Could not download dropped item: {exception.Message}");
        }
    }

    private void OnFilesKeyDown(object sender, KeyEventArgs args)
    {
        if (args.KeyModifiers != KeyModifiers.None)
            return;

        var character = GetSearchCharacter(args.Key);
        if (character is null || ViewModel?.Files is null)
            return;

        var now = DateTime.UtcNow;
        if (now - _lastSearchAt > TimeSpan.FromSeconds(1))
            _searchBuffer = string.Empty;

        _searchBuffer += character;
        _lastSearchAt = now;

        var file = ViewModel.Files.FirstOrDefault(item =>
            item.Name.StartsWith(_searchBuffer, StringComparison.OrdinalIgnoreCase));

        if (file is null && _searchBuffer.Length > 1)
        {
            _searchBuffer = character;
            file = ViewModel.Files.FirstOrDefault(item =>
                item.Name.StartsWith(_searchBuffer, StringComparison.OrdinalIgnoreCase));
        }

        if (file is not null)
        {
            FilesList.SelectedItems = new[] { file };
            FilesList.ScrollIntoView(file);
        }

        args.Handled = true;
    }
}
