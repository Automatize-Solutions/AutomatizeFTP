using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutomatizeFTP.Presentation.Avalonia.Views;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AutomatizeFTP.Presentation.Avalonia.Controls;

public sealed class FileExplorerListBox : ListBox
{
    private const double DragThreshold = 8;
    private IFileViewModel _selectionAnchor;
    private Point? _pointerStart;
    private PointerPressedEventArgs _pointerStartEvent;
    private bool _dragStarted;
    private string _searchBuffer = string.Empty;
    private DateTime _lastSearchAt = DateTime.MinValue;

    public FileExplorerListBox()
    {
        SelectionChanged += OnSelectionChanged;
        const RoutingStrategies pointerStrategies = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;
        AddHandler(PointerPressedEvent, HandlePointerPressed, pointerStrategies, handledEventsToo: true);
        AddHandler(PointerMovedEvent, HandlePointerMoved, pointerStrategies, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, HandlePointerReleased, pointerStrategies, handledEventsToo: true);
        AddHandler(InputElement.DoubleTappedEvent, HandleDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);
        DragDrop.SetAllowDrop(this, true);
        DragDrop.AddDragOverHandler(this, OnDragOver);
        DragDrop.AddDropHandler(this, OnDrop);
    }

    internal event EventHandler<FileExplorerDropEventArgs> FilesDropped;

    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override void OnKeyDown(KeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (args.KeyModifiers != KeyModifiers.None)
            return;

        var character = GetSearchCharacter(args.Key);
        var viewModel = GetViewModel(this);
        if (character is null || viewModel?.Files is null)
            return;

        var now = DateTime.UtcNow;
        if (now - _lastSearchAt > TimeSpan.FromSeconds(1))
            _searchBuffer = string.Empty;

        _searchBuffer += character;
        _lastSearchAt = now;

        var file = FindFile(viewModel, _searchBuffer);
        if (file is null && _searchBuffer.Length > 1)
        {
            _searchBuffer = character;
            file = FindFile(viewModel, _searchBuffer);
        }

        if (file is not null)
        {
            SelectedItems = new[] { file };
            ScrollIntoView(file);
        }

        args.Handled = true;
    }

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

    private static ICloudViewModel GetViewModel(Control control) => control.DataContext as ICloudViewModel;

    private static IFileViewModel FindFile(ICloudViewModel viewModel, string prefix) =>
        viewModel.Files.FirstOrDefault(item => item.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool CanDrop(
        FileDragPayload payload,
        ICloudViewModel target,
        IFileViewModel targetFolder)
    {
        if (payload is null || target is null)
            return false;

        if (payload.SourceProvider.Id != target.Id)
            return true;

        return targetFolder is not null &&
               payload.Items.All(item => !IsSameOrDescendant(item.Path, targetFolder.Path));
    }

    private static bool IsSameOrDescendant(string sourcePath, string targetPath)
    {
        var source = sourcePath.Replace('\\', '/').TrimEnd('/');
        var target = targetPath.Replace('\\', '/').TrimEnd('/');
        return string.Equals(source, target, StringComparison.OrdinalIgnoreCase) ||
               target.StartsWith(source + "/", StringComparison.OrdinalIgnoreCase);
    }

    private void HandlePointerPressed(object sender, PointerPressedEventArgs args)
    {
        if (args.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            return;

        Focus();
        _selectionAnchor = FindFileViewModel(args.Source);
        _pointerStart = args.GetPosition(this);
        _pointerStartEvent = args;
        _dragStarted = false;
        args.Pointer.Capture(this);
    }

    private void HandlePointerMoved(object sender, PointerEventArgs args)
    {
        if (_pointerStart is not { } start || _pointerStartEvent is null || _dragStarted)
            return;

        var point = args.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var current = point.Position;
        var hasPassedDragThreshold = Math.Abs(current.X - start.X) >= DragThreshold ||
                                     Math.Abs(current.Y - start.Y) >= DragThreshold;
        if (!hasPassedDragThreshold)
        {
            if (Bounds.Contains(current))
                SelectRangeAt(current);

            return;
        }

        _dragStarted = true;
        var dragStartEvent = _pointerStartEvent;
        _pointerStart = null;
        _pointerStartEvent = null;
        args.Pointer.Capture(null);
        args.Handled = true;
        _ = StartDragAsync(dragStartEvent, args.Pointer);
    }

    private void HandlePointerReleased(object sender, PointerReleasedEventArgs args)
    {
        _selectionAnchor = null;
        _pointerStart = null;
        _pointerStartEvent = null;
        _dragStarted = false;
        args.Pointer.Capture(null);
    }

    private void HandleDoubleTapped(object sender, TappedEventArgs args)
    {
        var viewModel = GetViewModel(this);
        var file = FindFileViewModel(args.Source) ?? viewModel?.SelectedFile;
        if (viewModel is null || file?.IsFolder != true)
            return;

        viewModel.SetSelectedFiles(new[] { file });
        viewModel.SetPath.Execute(Path.Combine(viewModel.CurrentPath, file.Name)).Subscribe();
        args.Handled = true;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        GetViewModel(this)?.SetSelectedFiles(SelectedItems.OfType<IFileViewModel>());
    }

    private void SelectRangeAt(Point point)
    {
        if (_selectionAnchor is null)
            return;

        var target = FindFileViewModel(this.GetVisualAt(point));
        if (target is null)
            return;

        var files = Items.OfType<IFileViewModel>().ToList();
        var anchorIndex = files.IndexOf(_selectionAnchor);
        var targetIndex = files.IndexOf(target);
        if (anchorIndex < 0 || targetIndex < 0)
            return;

        var start = Math.Min(anchorIndex, targetIndex);
        var count = Math.Abs(anchorIndex - targetIndex) + 1;
        SelectedItems = files.GetRange(start, count);
    }

    private void OnDragOver(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.From(args.DataTransfer);
        var target = GetViewModel(this);
        var targetFolder = GetDropTarget(args);
        var canDrop = CanDrop(payload, target, targetFolder);
        var isMove = canDrop && payload.SourceProvider.Id == target.Id;

        args.DragEffects = canDrop
            ? isMove ? DragDropEffects.Move : DragDropEffects.Copy
            : DragDropEffects.None;
        args.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs args)
    {
        var payload = FileDragPayload.From(args.DataTransfer);
        var target = GetViewModel(this);
        var targetFolder = GetDropTarget(args);
        if (!CanDrop(payload, target, targetFolder))
            return;

        payload = FileDragPayload.Take(args.DataTransfer);
        if (payload is null)
            return;

        var isMove = payload.SourceProvider.Id == target.Id;
        args.DragEffects = isMove ? DragDropEffects.Move : DragDropEffects.Copy;
        args.Handled = true;
        FilesDropped?.Invoke(this, new FileExplorerDropEventArgs(payload, targetFolder));
    }

    private IFileViewModel GetDropTarget(DragEventArgs args)
    {
        var target = FindFileViewModel(this.GetVisualAt(args.GetPosition(this)));
        return target?.IsFolder == true ? target : null;
    }

    private async Task StartDragAsync(PointerPressedEventArgs startEvent, IPointer pointer)
    {
        var viewModel = GetViewModel(this);
        var sourceFile = FindFileViewModel(startEvent.Source);
        if (viewModel is null || sourceFile is null)
            return;

        var selectedFiles = viewModel.SelectedFiles.Contains(sourceFile)
            ? viewModel.SelectedFiles
            : new[] { sourceFile };
        var items = selectedFiles
            .Select(file => new FileDragItem(file.Path, file.Name, file.IsFolder))
            .ToArray();
        var token = FileDragPayload.Register(new FileDragPayload(viewModel, items));
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText(token));

        try
        {
            await DragDrop.DoDragDropAsync(startEvent, transfer, DragDropEffects.Copy | DragDropEffects.Move);
        }
        finally
        {
            FileDragPayload.Release(token);
            pointer.Capture(null);
            _dragStarted = false;
        }
    }
}

internal sealed class FileExplorerDropEventArgs(FileDragPayload payload, IFileViewModel targetFolder) : EventArgs
{
    public FileDragPayload Payload { get; } = payload;

    public IFileViewModel TargetFolder { get; } = targetFolder;
}
