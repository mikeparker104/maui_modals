using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;

namespace Toolkit;

[StructLayout(LayoutKind.Sequential)]
struct DisplayRect
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential)]
struct MonitorInfo
{
    public uint cbSize;
    public DisplayRect rcMonitor;
    public DisplayRect rcWork;
    public uint dwFlags;
}

internal static class PlatformInterop
{
    const string User32DllName = "user32.dll";
    const int GWL_HWNDPARENT = -8;

    // See: https://github.com/microsoft/WindowsAppSDK/discussions/2603
    [DllImport(User32DllName, SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool EnableWindow(IntPtr hWnd, bool enabled);

    // https://github.com/dotnet/wpf/blob/ec69834f378fb98ef5f623db1c55610ac074001d/src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/NativeMethodsSetLastError.cs#L145
    [DllImport(User32DllName, EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
    static extern Int32 SetWindowLong(HandleRef hWnd, int nIndex, Int32 dwNewLong);

    // https://github.com/dotnet/wpf/blob/ec69834f378fb98ef5f623db1c55610ac074001d/src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/NativeMethodsSetLastError.cs#L154
    [DllImport(User32DllName, EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
    static extern IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport(User32DllName)]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport(User32DllName)]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    internal static bool ModalRunning { get; private set; }

    internal static void ConfigureAsModal(Microsoft.UI.Xaml.Window window)
    {
        var appWindows = Application.Current.Windows.TakeWhile(i => i.GetType() != typeof(ModalWindow)).ToList();
        var parentWindow = appWindows.Last()?.Handler.PlatformView as Microsoft.UI.Xaml.Window;

        // Set owner in order to set OverlappedPresenter.IsModal to true
        IntPtr targetWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        IntPtr ownerWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(parentWindow);
        SetWindowLong(new HandleRef(null, targetWindowHandle), GWL_HWNDPARENT, ownerWindowHandle);

        var appWindow = window.GetAppWindow();
        appWindow.IsShownInSwitchers = false;

        window.ExtendsContentIntoTitleBar = false;

        var presenter = appWindow.Presenter as OverlappedPresenter;

        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsModal = true;
    }

    internal static void RunModal()
    {
        if (ModalRunning)
            return;

        ModalRunning = true;
        DisableApplicationWindows(Application.Current.Windows.TakeWhile(i => i.GetType() != typeof(ModalWindow)).ToList());
    }

    internal static void StopModal()
    {
        if (!ModalRunning)
            return;

        ModalRunning = false;
        EnableApplicationWindows(Application.Current.Windows.TakeWhile(i => i.GetType() != typeof(ModalWindow)).ToList());
    }

    internal static Size GetAvailableScreenSize(Microsoft.UI.Xaml.Window window)
        => GetAvailableScreenSize(WinRT.Interop.WindowNative.GetWindowHandle(window));

    static Size GetAvailableScreenSize(IntPtr hwnd)
    {
        DisplayRect availableRect = new DisplayRect();
        IntPtr hMonitor = MonitorFromWindow(hwnd, 0);
        MonitorInfo monitorInfo = new MonitorInfo();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

        if (GetMonitorInfo(hMonitor, ref monitorInfo))
            availableRect = monitorInfo.rcWork;

        return new Size(availableRect.right, availableRect.bottom);
    }

    static void EnableApplicationWindows(IReadOnlyList<Window> applicationWindows)
        => SetApplicationWindowState(true, applicationWindows);

    static void DisableApplicationWindows(IReadOnlyList<Window> applicationWindows)
        => SetApplicationWindowState(false, applicationWindows);

    static void SetApplicationWindowState(bool enabled, IReadOnlyList<Window> applicationWindows)
    {
        var nativeWindows = GetNativeApplicationWindows(applicationWindows);

        foreach (var ptr in nativeWindows)
            EnableWindow(ptr, enabled);
    }

    static IReadOnlyList<IntPtr> GetNativeApplicationWindows(IReadOnlyList<Window> applicationWindows) => applicationWindows
        .Select(i => WinRT.Interop.WindowNative
            .GetWindowHandle(i.Handler.PlatformView as Microsoft.UI.Xaml.Window))
        .ToList();

    // https://github.com/dotnet/wpf/blob/ec69834f378fb98ef5f623db1c55610ac074001d/src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/UnsafeNativeMethodsOther.cs#L422
    static IntPtr SetWindowLong(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
    {
        IntPtr result = IntPtr.Zero;

        if (IntPtr.Size == 4)
        {
            // use SetWindowLong
            Int32 tempResult = SetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
            result = new IntPtr(tempResult);
        }
        else
        {
            // use SetWindowLongPtr
            result = SetWindowLongPtr(hWnd, nIndex, dwNewLong);
        }

        return result;
    }

    // https://github.com/dotnet/wpf/blob/ec69834f378fb98ef5f623db1c55610ac074001d/src/Microsoft.DotNet.Wpf/src/Shared/MS/Win32/NativeMethodsOther.cs#L350
    static int IntPtrToInt32(IntPtr intPtr)
        => unchecked((int)intPtr.ToInt64());
}