using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace AutomatizeFTP.Tests;

internal static class ReactiveUiInitializer
{
    // ReactiveUI 23 no longer initializes itself on first use: the app does it through
    // AppBuilder.UseReactiveUI, and without an equivalent the static constructors of
    // mixins such as ObservableLoggingMixin throw. Tests have no AppBuilder, so they
    // initialize the framework here, once per test assembly.
    [ModuleInitializer]
    internal static void Initialize() => RxAppBuilder
        .CreateReactiveUIBuilder()
        .WithPlatformServices()
        .WithCoreServices()
        .BuildApp();
}
