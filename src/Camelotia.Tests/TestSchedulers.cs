using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Camelotia.Tests;

internal static class TestSchedulers
{
    // Since ReactiveUI 22.1 a ReactiveCommand resolves its default output scheduler
    // from RxSchedulers.MainThreadScheduler rather than from RxApp.MainThreadScheduler.
    // RxSchedulers performs no unit test detection, so it stays on DefaultScheduler and
    // commands surface their results on the thread pool. Every assertion that reads a
    // view model right after Execute() then races the pool and fails intermittently,
    // no matter what the tests assign to RxApp.MainThreadScheduler.
    // See https://github.com/reactiveui/ReactiveUI/issues/4183 -- remove once fixed.
    [ModuleInitializer]
    internal static void UseImmediateSchedulers()
    {
        RxSchedulers.MainThreadScheduler = ImmediateScheduler.Instance;
        RxSchedulers.TaskpoolScheduler = ImmediateScheduler.Instance;
    }
}
