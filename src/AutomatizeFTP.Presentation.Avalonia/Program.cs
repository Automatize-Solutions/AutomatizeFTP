using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using ReactiveUI.Avalonia;
using Velopack;
using Velopack.Sources;

namespace AutomatizeFTP.Presentation.Avalonia;

public static class Program
{
    private const string ReleasesRepository = "https://github.com/Automatize-Solutions/AutomatizeFTP";

    public static void Main(string[] args)
    {
        // Velopack hook: must run before anything else. It handles the
        // install/update/uninstall callbacks and exits the process early when
        // the app is being invoked by the updater instead of the user.
        VelopackApp.Build().Run();

        _ = Task.Run(DownloadPendingUpdatesAsync);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseReactiveUI(reactiveUi => reactiveUi
                .WithExceptionHandler(Observer.Create<Exception>(Console.WriteLine)))
            .UsePlatformDetect()
            .LogToTrace();

    private static async Task DownloadPendingUpdatesAsync()
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(ReleasesRepository, null, prerelease: false));
            if (!manager.IsInstalled)
                return; // dev run or portable build: nothing to update.

            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            await manager.DownloadUpdatesAsync(update).ConfigureAwait(false);

            // Apply silently after the user quits; next launch runs the new version.
            manager.WaitExitThenApplyUpdates(update, silent: true, restart: false);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Update check failed: {exception.Message}");
        }
    }
}
