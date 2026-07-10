using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Controls;
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
                .Where(args => ViewModel.IsFolder)
                .Do(args => ViewModel.Provider.SetSelectedFiles(new[] { ViewModel }))
                .Select(args => Path.Combine(ViewModel.Provider.CurrentPath, ViewModel.Name))
                .InvokeCommand(this, x => x.ViewModel.Provider.SetPath)
                .DisposeWith(disposables);

            ContextMenu
                .Events()
                .Opening
                .Subscribe(args =>
                {
                    ContextMenu.DataContext = ViewModel;
                    foreach (var menuItem in ContextMenu.Items.OfType<MenuItem>())
                        menuItem.DataContext = ViewModel;
                    CreateFolderMenuItem.Command = ViewModel.Provider.Folder.Open;
                    CreateFolderAndEnterMenuItem.Command = ViewModel.Provider.Folder.OpenAndEnter;
                    ViewModel.Provider.SetSelectedFiles(new[] { ViewModel });
                })
                .DisposeWith(disposables);
        });
    }
}
