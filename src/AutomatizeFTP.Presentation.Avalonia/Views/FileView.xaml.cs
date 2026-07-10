using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class FileView : ReactiveUserControl<IFileViewModel>
{
    private Point? _dragStart;
    private PointerPressedEventArgs _dragStartEvent;

    public FileView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.Events()
                .DoubleTapped
                .Where(args => ViewModel.IsFolder)
                .Do(args => ViewModel.Provider.SelectedFile = ViewModel)
                .Select(args => Path.Combine(ViewModel.Provider.CurrentPath, ViewModel.Name))
                .InvokeCommand(this, x => x.ViewModel.Provider.SetPath)
                .DisposeWith(disposables);

            ContextMenu
                .Events()
                .Opening
                .Subscribe(args => ViewModel.Provider.SelectedFile = ViewModel)
                .DisposeWith(disposables);
        });
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs args)
    {
        if (args.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            return;

        _dragStart = args.GetPosition(this);
        _dragStartEvent = args;
        args.Pointer.Capture(this);
    }

    private async void OnPointerMoved(object sender, PointerEventArgs args)
    {
        if (_dragStart is not { } start || _dragStartEvent is null || args.Pointer.Captured != this)
            return;

        var point = args.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        if (FindAncestorListBox() is { } list && list.Bounds.Contains(args.GetPosition(list)))
            return;

        var current = point.Position;
        if (Math.Abs(current.X - start.X) < 8 && Math.Abs(current.Y - start.Y) < 8)
            return;

        var dragStartEvent = _dragStartEvent;
        _dragStart = null;
        _dragStartEvent = null;

        var token = FileDragPayload.Register(
            new FileDragPayload(ViewModel.Provider, ViewModel.Path, ViewModel.Name, ViewModel.IsFolder));
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText(token));

        args.Pointer.Capture(null);
        try
        {
            await DragDrop.DoDragDropAsync(dragStartEvent, transfer, DragDropEffects.Copy);
            args.Handled = true;
        }
        finally
        {
            FileDragPayload.Release(token);
            args.Pointer.Capture(null);
        }
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs args)
    {
        _dragStart = null;
        _dragStartEvent = null;
        args.Pointer.Capture(null);
    }

    private ListBox FindAncestorListBox()
    {
        for (var control = Parent as Control; control is not null; control = control.Parent as Control)
        {
            if (control is ListBox list)
                return list;
        }

        return null;
    }
}
