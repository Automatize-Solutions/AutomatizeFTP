using System;
using System.Reactive;
using Avalonia;
using ReactiveUI.Avalonia;
using Velopack;

namespace AutomatizeFTP.Presentation.Avalonia;

public static class Program
{
    public static void Main(string[] args)
    {
        // Velopack hook: must run before anything else. It handles the
        // install/update/uninstall callbacks and exits the process early when
        // the app is being invoked by the updater instead of the user.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseReactiveUI(reactiveUi => reactiveUi
                .WithExceptionHandler(Observer.Create<Exception>(Console.WriteLine)))
            .UsePlatformDetect()
            .LogToTrace();
}
