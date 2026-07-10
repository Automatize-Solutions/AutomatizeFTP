using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class FileView : ReactiveUserControl<IFileViewModel>
{
    private static async Task<bool> ConfirmDeleteAsync(Window owner, string fileName)
    {
        var dialog = new Window
        {
            Title = GetResource("DeleteConfirmationTitle"),
            Background = GetResourceObject("SurfaceBrush") as IBrush,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var cancelButton = new Button
        {
            Content = GetResource("Close"),
            Background = GetResourceObject("SurfaceSubtleBrush") as IBrush,
            BorderBrush = GetResourceObject("BorderBrush") as IBrush,
            Foreground = GetResourceObject("TextPrimaryBrush") as IBrush,
            MinWidth = 90,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        var confirmButton = new Button
        {
            Content = GetResource("Confirm"),
            Background = GetResourceObject("AccentBrush") as IBrush,
            BorderBrush = GetResourceObject("AccentBrush") as IBrush,
            Foreground = GetResourceObject("AccentTextBrush") as IBrush,
            MinWidth = 90,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Classes = { "accent" }
        };
        cancelButton.Click += (_, _) => dialog.Close(false);
        confirmButton.Click += (_, _) => dialog.Close(true);

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = string.Format(
                        CultureInfo.CurrentCulture,
                        GetResource("DeleteConfirmationPrompt"),
                        fileName),
                    Foreground = GetResourceObject("TextPrimaryBrush") as IBrush,
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancelButton, confirmButton }
                }
            }
        };

        return await dialog.ShowDialog<bool>(owner);
    }

    private static string GetResource(string key) =>
        GetResourceObject(key)?.ToString() ?? key;

    private static object GetResourceObject(string key)
    {
        var resources = Application.Current?.Resources;
        return resources is not null &&
               resources.TryGetResource(key, ThemeVariant.Default, out var resource)
            ? resource
            : null;
    }

    public FileView()
    {
        InitializeComponent();
        DeleteMenuItem.Click += OnDeleteMenuItemClick;
        this.WhenActivated(disposables =>
        {
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

    private async void OnDeleteMenuItemClick(object sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null || ViewModel?.Provider is null)
            return;

        if (!await ConfirmDeleteAsync(owner, ViewModel.Name))
            return;

        try
        {
            await ViewModel.Provider.DeleteSelectedFile.Execute().ToTask();
        }
        catch (Exception exception)
        {
            ViewModel.Provider.ReportError(exception);
        }
    }
}
