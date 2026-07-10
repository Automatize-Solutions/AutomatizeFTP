using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

/// <summary>
/// Outside an .app bundle (dotnet run, VS Code F5) macOS shows the generic
/// "exec" icon in the dock; AppKit lets the process replace it at runtime
/// through NSApplication.applicationIconImage.
/// </summary>
internal static class MacDockIcon
{
    public static void TrySet(string imagePath)
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists(imagePath))
            return;

        var utf8Path = Marshal.StringToCoTaskMemUTF8(imagePath);
        try
        {
            var nsPath = NativeMethods.Send(NativeMethods.GetClass("NSString"), NativeMethods.Selector("stringWithUTF8String:"), utf8Path);
            var image = NativeMethods.Send(NativeMethods.Send(NativeMethods.GetClass("NSImage"), NativeMethods.Selector("alloc")), NativeMethods.Selector("initWithContentsOfFile:"), nsPath);
            if (image == IntPtr.Zero)
                return;

            var app = NativeMethods.Send(NativeMethods.GetClass("NSApplication"), NativeMethods.Selector("sharedApplication"));
            NativeMethods.Send(app, NativeMethods.Selector("setApplicationIconImage:"), image);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8Path);
        }
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
    }
}
