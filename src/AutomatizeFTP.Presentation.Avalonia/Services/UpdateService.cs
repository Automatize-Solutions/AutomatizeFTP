using Avalonia.Threading;
using Velopack;
using Velopack.Sources;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class UpdateService
{
    private const string ReleasesRepository = "https://github.com/Automatize-Solutions/AutomatizeFTP";

    public enum UpdatePhase
    {
        Idle,
        Downloading,
        Ready
    }

    public static UpdateService Instance { get; } = new();

    private UpdateManager _manager;
    private UpdateInfo _update;
    private bool _restarting;

    public event EventHandler StatusChanged;

    public UpdatePhase Phase { get; private set; }

    public int DownloadPercent { get; private set; }

    public string AvailableVersion => _update?.TargetFullRelease?.Version?.ToString();

    public async Task RunAsync()
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(ReleasesRepository, null, prerelease: false));
            if (!manager.IsInstalled)
                return; // dev run or portable build: nothing to update.

            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            _manager = manager;
            _update = update;
            SetStatus(UpdatePhase.Downloading, 0);

            var activity = MacAppNap.Begin("Downloading application update");
            try
            {
                await manager.DownloadUpdatesAsync(update, percent => SetStatus(UpdatePhase.Downloading, percent))
                    .ConfigureAwait(false);
            }
            finally
            {
                MacAppNap.End(activity);
            }

            SetStatus(UpdatePhase.Ready, 100);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Update check failed: {exception.Message}");
            SetStatus(UpdatePhase.Idle, 0);
        }
    }

    /// <summary>Applies the downloaded update immediately and relaunches the app.</summary>
    public void RestartNow()
    {
        if (Phase != UpdatePhase.Ready)
            return;

        _restarting = true;
        _manager.ApplyUpdatesAndRestart(_update);
    }

    /// <summary>
    /// Spawns the updater on app exit so the pending update is applied after the
    /// process quits; the next launch runs the new version. Scheduled here instead
    /// of right after the download so it never races RestartNow's updater.
    /// </summary>
    public void ApplyPendingOnExit()
    {
        if (Phase != UpdatePhase.Ready || _restarting)
            return;

        _manager.WaitExitThenApplyUpdates(_update, silent: true, restart: false);
    }

    private void SetStatus(UpdatePhase phase, int percent)
    {
        Phase = phase;
        DownloadPercent = percent;
        Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(this, EventArgs.Empty));
    }
}
