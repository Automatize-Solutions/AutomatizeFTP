using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.Interfaces;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class FileView : ReactiveUserControl<IFileViewModel>
{
    public FileView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.Events()
                .DoubleTapped
                .Do(args => ViewModel.Provider.SelectedFile = ViewModel)
                .Select(args => Unit.Default)
                .InvokeCommand(this, x => x.ViewModel.Provider.Open)
                .DisposeWith(disposables);

            ContextMenu
                .Events()
                .Opening
                .Subscribe(args => ViewModel.Provider.SelectedFile = ViewModel)
                .DisposeWith(disposables);
        });
    }
}
