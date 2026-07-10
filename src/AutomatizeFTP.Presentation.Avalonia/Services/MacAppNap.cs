using System.Runtime.InteropServices;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

/// <summary>
/// App Nap throttles processes whose windows are not visible, dropping the
/// update download from MB/s to ~35 kB/s when the app sits in the background.
/// An NSProcessInfo activity marks the work as user-initiated so macOS keeps
/// full network speed until it ends.
/// </summary>
internal static class MacAppNap
{
    // NSActivityUserInitiatedAllowingIdleSystemSleep: exempt from App Nap
    // without keeping the machine awake.
    private const ulong UserInitiatedAllowingIdleSystemSleep = 0x00FFFFFF;

    public static IntPtr Begin(string reason)
    {
        if (!OperatingSystem.IsMacOS())
            return IntPtr.Zero;

        var utf8Reason = Marshal.StringToCoTaskMemUTF8(reason);
        try
        {
            var nsReason = NativeMethods.Send(NativeMethods.GetClass("NSString"), NativeMethods.Selector("stringWithUTF8String:"), utf8Reason);
            var processInfo = NativeMethods.Send(NativeMethods.GetClass("NSProcessInfo"), NativeMethods.Selector("processInfo"));
            var activity = NativeMethods.Send(processInfo, NativeMethods.Selector("beginActivityWithOptions:reason:"), UserInitiatedAllowingIdleSystemSleep, nsReason);
            if (activity == IntPtr.Zero)
                return IntPtr.Zero;

            // The token comes back autoreleased; retain it so it survives for
            // the whole download on a thread without an autorelease pool.
            return NativeMethods.Send(activity, NativeMethods.Selector("retain"));
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8Reason);
        }
    }

    public static void End(IntPtr activity)
    {
        if (activity == IntPtr.Zero)
            return;

        var processInfo = NativeMethods.Send(NativeMethods.GetClass("NSProcessInfo"), NativeMethods.Selector("processInfo"));
        NativeMethods.Send(processInfo, NativeMethods.Selector("endActivity:"), activity);
        NativeMethods.Send(activity, NativeMethods.Selector("release"));
    }

    private static class NativeMethods
    {
        private const string LibObjC = "/usr/lib/libobjc.dylib";

        [DllImport(LibObjC, EntryPoint = "objc_getClass", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr GetClass([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(LibObjC, EntryPoint = "sel_registerName", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr Selector([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr Send(IntPtr receiver, IntPtr selector, ulong options, IntPtr reason);
    }
}
