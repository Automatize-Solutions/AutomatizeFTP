using System;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Avalonia.Services;
using AutomatizeFTP.Presentation.Avalonia.Views;
using AutomatizeFTP.Presentation.Infrastructure;
using AutomatizeFTP.Presentation.ViewModels;
using AutomatizeFTP.Services;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia;

public class App : Application
{
    private readonly NewtonsoftJsonSuspensionDriver _driver = new("appstate.json");
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
        styles.UseTheme(_state.Theme);
        styles.ThemeChanged += (_, _) => _state.Theme = styles.CurrentTheme.ToString();
        view.SwitchThemeButton.Click += (_, _) => styles.UseNextTheme();
        view.LanguageButton.Click += (_, _) => LocalizationManager.Instance.ToggleLanguage();
        LocalizationManager.Instance.LanguageChanged += (_, _) => _state.Language = LocalizationManager.Instance.CurrentLanguage;
        view.DataContext ??= CreateViewModel(window);
        return view;
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
            scheduler);

        var localState = new CloudState { Type = CloudType.Local };
        var localProvider = CreateCloudViewModel(localState, factory.CreateCloud(localState.Parameters));

        return new MainViewModel(
            main,
            factory,
            CreateCloudViewModel,
            scheduler,
            localProvider);
    }
}
