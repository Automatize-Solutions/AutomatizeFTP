using System;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Avalonia.Services;
using AutomatizeFTP.Presentation.Avalonia.Views;
using AutomatizeFTP.Presentation.Infrastructure;
using AutomatizeFTP.Presentation.ViewModels;
using AutomatizeFTP.Services;
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

        var suspension = new AutoSuspendHelper(ApplicationLifetime);
        suspension.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownRequested += (_, _) => _driver.SaveState(_state).Subscribe();

        var window = new Window
        {
            Height = 590,
            Width = 850,
            MinHeight = 590,
            MinWidth = 850,
        };

        AttachDevTools(window);
        window.Content = CreateView(window);
        window.Show();

        base.OnFrameworkInitializationCompleted();
    }

    public object CreateView(Window window)
    {
        var view = new MainView();
        var styles = new AvaloniaStyleManager(view);
        view.SwitchThemeButton.Click += (_, _) => styles.UseNextTheme();
        view.DataContext ??= CreateViewModel(window);
        return view;
    }

    private static void AttachDevTools(TopLevel window)
    {
#if DEBUG
        window.AttachDevTools();
#endif
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

    private MainViewModel CreateViewModel(Window window)
    {
        var main = _state;
        var scheduler = AvaloniaScheduler.Instance;
        return new MainViewModel(
            main,
            new CloudFactory(),
            (state, provider) => new CloudViewModel(
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
                scheduler),
            scheduler);
    }
}
