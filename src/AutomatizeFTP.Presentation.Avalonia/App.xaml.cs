using System;
using System.IO;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Avalonia.Services;
using AutomatizeFTP.Presentation.Avalonia.Views;
using AutomatizeFTP.Presentation.Infrastructure;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Presentation.ViewModels;
using AutomatizeFTP.Services;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia;

public class App : Application
{
    private readonly NewtonsoftJsonSuspensionDriver _driver = new(GetStateFilePath());
    private MainState _state;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // ReactiveUI 23 seeds ISuspensionHost.AppState with Unit and only fills it in
        // when it is null, so GetAppState<MainState>() can never succeed. Drive the
        // suspension driver directly instead: it is the only thing we ever wanted.
        _state = LoadState();

        LocalizationManager.Instance.UseLanguage(_state.Language);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Exit += (_, _) => SaveState();

        var window = new Window
        {
            Title = "AutomatizeFTP · FTP Workspace",
            Height = 820,
            Width = 1400,
            MinHeight = 680,
            MinWidth = 1180,
        };

        window.Content = CreateView(window);
        window.Show();

        base.OnFrameworkInitializationCompleted();
    }

    public object CreateView(Window window)
    {
        var view = new MainView();
        var styles = new AvaloniaStyleManager();
        styles.ThemeChanged += (_, _) =>
        {
            _state.Theme = styles.CurrentTheme.ToString();
            UpdateThemeButton(view, styles.CurrentTheme);
            SaveState();
        };
        styles.UseTheme(_state.Theme);
        UpdateThemeButton(view, styles.CurrentTheme);
        view.SwitchThemeButton.Click += (_, _) => styles.UseNextTheme();
        view.LanguageButton.Click += (_, _) => LocalizationManager.Instance.ToggleLanguage();
        LocalizationManager.Instance.LanguageChanged += (_, _) => _state.Language = LocalizationManager.Instance.CurrentLanguage;
        view.DataContext ??= CreateViewModel(window);
        view.ClearTransferQueueButton.Click += async (_, _) =>
        {
            var main = (MainViewModel)view.DataContext;
            if (main.TransferQueue.HasActiveItems && !await ConfirmClearTransferQueueAsync(window))
                return;

            await main.TransferQueue.ClearAsync();
        };
        return view;
    }

    private static async Task<bool> ConfirmClearTransferQueueAsync(Window owner)
    {
        var dialog = new Window
        {
            Title = GetResource("ClearTransferQueueTitle"),
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
            Margin = new global::Avalonia.Thickness(24),
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = GetResource("ClearTransferQueuePrompt"),
                    Foreground = GetResourceObject("TextPrimaryBrush") as IBrush,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
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

    private static void UpdateThemeButton(MainView view, AvaloniaStyleManager.Theme currentTheme)
    {
        var isLight = currentTheme == AvaloniaStyleManager.Theme.Light;
        view.ThemeButtonIcon.Text = isLight ? "☾" : "☀";
        view.ThemeButtonLabel.Text = isLight
            ? nameof(AvaloniaStyleManager.Theme.Dark)
            : nameof(AvaloniaStyleManager.Theme.Light);
    }

    private static object GetResourceObject(string key)
    {
        var resources = Current?.Resources;
        return resources is not null &&
               resources.TryGetResource(key, ThemeVariant.Default, out var resource)
            ? resource
            : null;
    }

    private static string GetStateFilePath()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var stateDirectory = string.IsNullOrWhiteSpace(applicationData)
            ? AppContext.BaseDirectory
            : Path.Combine(applicationData, "AutomatizeFTP");

        Directory.CreateDirectory(stateDirectory);
        var statePath = Path.Combine(stateDirectory, "appstate.json");
        var legacyPath = Path.GetFullPath("appstate.json");

        if (!File.Exists(statePath) && File.Exists(legacyPath) &&
            !string.Equals(statePath, legacyPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(legacyPath, statePath);
        }

        return statePath;
    }

    private MainState LoadState()
    {
        try
        {
            return (MainState)_driver.LoadState().Wait();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not restore app state, starting fresh: {exception.Message}");
            return new MainState();
        }
    }

    private void SaveState()
    {
        try
        {
            _driver.SaveState(_state).Subscribe();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not save app state: {exception.Message}");
        }
    }

    private MainViewModel CreateViewModel(Window window)
    {
        var main = _state;
        var scheduler = AvaloniaScheduler.Instance;
        var factory = new CloudFactory();
        var transferQueue = new TransferQueue();
        ICloudViewModel localProvider = null;

        CloudViewModel CreateCloudViewModel(CloudState state, ICloud provider) => new(
            state,
            owner => new CreateFolderViewModel(state.CreateFolderState, owner, provider, scheduler),
            owner => new RenameFileViewModel(state.RenameFileState, owner, provider, scheduler),
            (file, owner) => new FileViewModel(owner, file),
            (folder, owner) => new FolderViewModel(owner, folder),
            new AuthViewModel(
                new HostAuthViewModel(state.AuthState.HostAuthState, provider, scheduler),
                provider,
                scheduler),
            new AvaloniaFileManager(window),
            provider,
            scheduler,
            localProvider,
            transferQueue);

        var localPath = ResolveLocalPath(_state.LocalPath);
        _state.LocalPath = localPath;

        var localState = new CloudState
        {
            Type = CloudType.Local,
            CurrentPath = localPath
        };
        localProvider = CreateCloudViewModel(localState, factory.CreateCloud(localState.Parameters));

        return new MainViewModel(
            main,
            factory,
            CreateCloudViewModel,
            scheduler,
            localProvider,
            transferQueue);
    }

    private string ResolveLocalPath(string savedPath)
    {
        if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
            return savedPath;

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktopPath))
        {
            Directory.CreateDirectory(desktopPath);
            return desktopPath;
        }

        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userProfilePath)
            ? Directory.GetCurrentDirectory()
            : userProfilePath;
    }
}
