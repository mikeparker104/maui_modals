using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;

namespace Toolkit;

enum NSWindowButton : ulong
{
    CloseButton,
    MiniaturizeButton,
    ZoomButton
}

internal static class PlatformInterop
{
    // See: https://gist.github.com/rolfbjarne/981b778a99425a6e630c
    const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    extern static void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    extern static IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSend(IntPtr handle, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSend(IntPtr handle, IntPtr selector, IntPtr arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend_stret")]
    extern static void RectangleF_objc_msgSend_stret(out CGRect retval, IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSendCGPoint(IntPtr receiver, IntPtr selector, CGPoint point);

    static NativeHandle? _nsApplicationHandle;
    static NativeHandle NSApplicationHandle => _nsApplicationHandle ??= Class.GetHandle("NSApplication");

    static Selector _sharedApplicationSelector;
    static Selector SharedApplicationSelector => _sharedApplicationSelector ??= new Selector("sharedApplication");

    static Selector _windowsSelector;
    static Selector WindowsSelector => _windowsSelector ??= new Selector("windows");

    static Selector _standardWindowButtonSelector;
    static Selector StandardWindowButtonSelector => _standardWindowButtonSelector ??= new Selector("standardWindowButton:");

    static Selector _standardWindowButtonSetHiddenSelector;
    static Selector StandardWindowButtonSetHiddenSelector => _standardWindowButtonSetHiddenSelector ??= new Selector("setHidden:");

    static Selector _runModalForWindowSelector;
    static Selector RunModalForWindowSelector => _runModalForWindowSelector ??= new Selector("runModalForWindow:");

    static Selector _stopModalSelector;
    static Selector StopModalSelector => _stopModalSelector ??= new Selector("stopModal");

    static Selector _screenSelector;
    static Selector ScreenSelector => _screenSelector ??= new Selector("screen");

    static Selector _visibleFrameSelector;
    static Selector VisibleFrameSelector => _visibleFrameSelector ??= new Selector("visibleFrame");

    static Selector _setFrameOriginSelector;
    static Selector SetFrameOriginSelector => _setFrameOriginSelector ??= new Selector("setFrameOrigin:");

    static Selector _frameSelector;
    static Selector FrameSelector => _frameSelector ??= new Selector("frame");

    internal static bool ModalRunning { get; private set; }

    internal static NSObject GetNSWindow(this UIWindow uiWindow)
    {
        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);
        var currentWindows = sharedApplication.PerformSelector(WindowsSelector) as NSArray;

        foreach (var nsWindow in currentWindows)
        {
            if (!nsWindow.Description.Contains("UINSWindow"))
                continue;

            var uiWindows = (nsWindow.ValueForKey(new NSString("uiWindows")) as NSArray)?.Cast<UIWindow>() ?? Enumerable.Empty<UIWindow>();

            if (uiWindows.Contains(uiWindow))
                return nsWindow;
        }

        return null;
    }

    internal static async Task<NSObject> GetNSWindowWhenAdded()
    {
        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);

        var currentWindows = sharedApplication.PerformSelector(WindowsSelector) as NSArray;
        NSArray newWindows = default;

        var count = 0;
        var maxCount = 100;

        while (count < maxCount)
        {
            newWindows = sharedApplication.PerformSelector(WindowsSelector) as NSArray;

            if (newWindows.Count != currentWindows.Count)
                break;

            await Task.Delay(1);
            count++;
        }

        if (currentWindows.Count == newWindows.Count)
            return null;

        var addedNSWindow = newWindows.Except(currentWindows).First();

        return addedNSWindow;
    }

    internal static void ConfigureAsModal(NSObject nsWindow)
    {
        if (nsWindow == null)
            return;

        var minimizeButton = Runtime.GetNSObject(
            IntPtr_objc_msgSend_IntPtr(
                nsWindow.Handle,
                StandardWindowButtonSelector.Handle,
                (IntPtr)(ulong)NSWindowButton.MiniaturizeButton));

        void_objc_msgSend_bool(
            minimizeButton.Handle,
            StandardWindowButtonSetHiddenSelector.Handle,
            true);

        var zoomButton = Runtime.GetNSObject(
            IntPtr_objc_msgSend_IntPtr(
                nsWindow.Handle,
                StandardWindowButtonSelector.Handle,
                (IntPtr)(ulong)NSWindowButton.ZoomButton));

        void_objc_msgSend_bool(
            zoomButton.Handle,
            StandardWindowButtonSetHiddenSelector.Handle,
            true);

        CenterWindow(nsWindow);
    }

    internal static void RunModal(NSObject nsWindow)
    {
        if (nsWindow == null)
            return;

        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);

        objc_msgSend(
            sharedApplication.Handle,
            RunModalForWindowSelector.Handle,
            nsWindow.Handle);

        ModalRunning = true;
    }

    internal static void StopModal()
    {
        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);

        objc_msgSend(
            sharedApplication.Handle,
            StopModalSelector.Handle);

        ModalRunning = false;
    }

    static Size GetAvailableScreenSize(NSObject nsWindow)
    {
        // Get the screen that the window is displayed on
        NSObject screen = Runtime.GetNSObject(IntPtr_objc_msgSend(nsWindow.Handle, ScreenSelector.Handle));

        if (screen == null)
            return default;

        RectangleF_objc_msgSend_stret(out CGRect availableScreen, screen.Handle, VisibleFrameSelector.Handle);

        if (availableScreen == default)
            return default;

        return new Size(availableScreen.Size.Width, availableScreen.Size.Height);
    }

    static Size GetWindowSize(NSObject nsWindow)
    {
        RectangleF_objc_msgSend_stret(out CGRect windowSize, nsWindow.Handle, FrameSelector.Handle);

        if (windowSize == default)
            return default;

        return new Size(windowSize.Size.Width, windowSize.Size.Height);
    } 

    internal static void CenterWindow(NSObject nsWindow)
    {
        if (!nsWindow.RespondsToSelector(SetFrameOriginSelector))
            return;

        var screenSize = GetAvailableScreenSize(nsWindow);

        if (screenSize == default)
            return;

        var windowSize = GetWindowSize(nsWindow);

        if (windowSize == default)
            return;

        var screenCenterX = screenSize.Width / 2;
        var screenCenterY = screenSize.Height / 2;

        var windowX = screenCenterX - (windowSize.Width / 2.0);
        var windowY = screenCenterY - (windowSize.Height / 2.0);

        objc_msgSendCGPoint(nsWindow.Handle, SetFrameOriginSelector.Handle, new CGPoint(windowX, windowY));
    }
}