using System;
using System.Reactive;
using Avalonia;
using ReactiveUI.Avalonia;

namespace Camelotia.Presentation.Avalonia;

public static class Program
{
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseReactiveUI(reactiveUi => reactiveUi
                .WithExceptionHandler(Observer.Create<Exception>(Console.WriteLine)))
            .UsePlatformDetect()
            .LogToTrace();
}
