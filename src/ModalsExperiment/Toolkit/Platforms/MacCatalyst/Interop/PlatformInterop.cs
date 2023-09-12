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
    static extern void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSend(IntPtr handle, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSend(IntPtr handle, IntPtr selector, IntPtr arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend_stret")]
    static extern void RectangleF_objc_msgSend_stret(out CGRect retval, IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern CGRect CGRect_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    static extern void objc_msgSendCGPoint(IntPtr receiver, IntPtr selector, CGPoint point);

    static NativeHandle? _nsApplicationHandle;
    static NativeHandle NSApplicationHandle => _nsApplicationHandle ??= Class.GetHandle("NSApplication");

    static NativeHandle? _nsScreenHandle;
    static NativeHandle NSScreenHandle => _nsScreenHandle ??= Class.GetHandle("NSScreen");

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

    static Selector _screensSelector;
    static Selector ScreensSelector => _screensSelector ??= new Selector("screens");

    static Selector _screenSelector;
    static Selector ScreenSelector => _screenSelector ??= new Selector("screen");

    static Selector _visibleFrameSelector;
    static Selector VisibleFrameSelector => _visibleFrameSelector ??= new Selector("visibleFrame");

    static Selector _setFrameOriginSelector;
    static Selector SetFrameOriginSelector => _setFrameOriginSelector ??= new Selector("setFrameOrigin:");

    static Selector _frameSelector;
    static Selector FrameSelector => _frameSelector ??= new Selector("frame");

    internal static bool ModalRunning { get; private set; }

    internal static int GetScreenIndex(this UIWindow uiWindow)
    {
        var nsWindow = uiWindow.GetNSWindow();

        if (nsWindow == null)
            return -1;

        var nsScreen = Runtime.GetNSObject(NSScreenHandle);
        var screens = nsScreen.PerformSelector(ScreensSelector) as NSArray;

        var screen = nsWindow.PerformSelector(ScreenSelector);

        var screenCount = screens.Count;

        for (nuint i = 0; i < screenCount; i++)
        {
            var screenHandle = screens.ValueAt(i);
            var screenObject = Runtime.GetNSObject(screenHandle);

            if (screenObject == screen)
                return (int)i;
        }

        return -1;
    }

    internal static NSObject GetNSWindow(this UIWindow uiWindow)
    {
        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);
        var currentWindows = sharedApplication.PerformSelector(WindowsSelector) as NSArray;

        foreach (var nsWindow in currentWindows)
        {
            if (!nsWindow.Description.Contains("UINSWindow"))
                continue;

            // NOTE: Accessing UINSWindow.uiWindows will result in the following warning:
            // WARNING: SPI usage of '-[UINSWindow uiWindows]' is being shimmed. This will break in the future. Please file a radar requesting API for what you are trying to do.
            // Use of SPIs (System Programming Interfaces) in Mac Catalyst applications are not officially supported and may lead to compatibility issues in future.
            var uiWindows = (nsWindow.ValueForKey(new NSString("uiWindows")) as NSArray)?.Cast<UIWindow>() ?? Enumerable.Empty<UIWindow>();

            if (uiWindows.Contains(uiWindow))
                return nsWindow;
        }

        return null;
    }

    internal static async Task<NSObject> GetNSWindowWhenAdded(this UIWindow uiWindow)
    {
        var nsWindow = uiWindow.GetNSWindow();

        if (nsWindow != null)
            return nsWindow;

        var nsApplication = Runtime.GetNSObject(NSApplicationHandle);
        var sharedApplication = nsApplication.PerformSelector(SharedApplicationSelector);

        var currentWindows = sharedApplication.PerformSelector(WindowsSelector) as NSArray;
        NSArray newWindows = default;

        var count = 0;

#if DEBUG
        var maxCount = 10000;
#else
        var maxCount = 2000;
#endif

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
        nsWindow = uiWindow.GetNSWindow();

        return addedNSWindow.Handle == nsWindow.Handle ? nsWindow : null;
    }

    internal static void ConfigureAsModal(NSObject nsWindow, int screenIndex = -1)
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

        CenterWindow(nsWindow, screenIndex);
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

    internal static void CenterWindow(NSObject nsWindow, int screenIndex = -1)
    {
        if (!nsWindow.RespondsToSelector(SetFrameOriginSelector))
            return;

        var visibleFrame = screenIndex != -1 ? GetVisibleFrame(screenIndex) : GetVisibleFrame(nsWindow);

        if (visibleFrame == default)
            return;

        var windowSize = GetWindowSize(nsWindow);

        if (windowSize == default)
            return;

        var windowX = visibleFrame.X + (visibleFrame.Width - windowSize.Width) / 2;
        var windowY = visibleFrame.Y + (visibleFrame.Height - windowSize.Height) / 2;

        objc_msgSendCGPoint(nsWindow.Handle, SetFrameOriginSelector.Handle, new CGPoint(windowX, windowY));
    }

    static NSObject GetScreenAtIndex(int index)
    {
        var nsScreen = Runtime.GetNSObject(NSScreenHandle);
        var screens = nsScreen.PerformSelector(ScreensSelector) as NSArray;

        var requestedIndex = (nuint)index;
        var screenCount = screens.Count;

        if (requestedIndex < 0 || requestedIndex >= screenCount)
            return null;

        var screenHandle = screens.ValueAt(requestedIndex);
        var screen = Runtime.GetNSObject(screenHandle);

        return screen;
    }

    static CGRect GetVisibleFrame(NSObject nsWindow)
    {
        // Get the screen that the window is displayed on
        NSObject screen = Runtime.GetNSObject(IntPtr_objc_msgSend(nsWindow.Handle, ScreenSelector.Handle));

        if (screen == null)
            return default;

        CGRect availableScreen;

        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            RectangleF_objc_msgSend_stret(out availableScreen, screen.Handle, VisibleFrameSelector.Handle);
        else
            availableScreen = CGRect_objc_msgSend(screen.Handle, VisibleFrameSelector.Handle);

        if (availableScreen == default)
            return default;

        return availableScreen;
    }

    static CGRect GetVisibleFrame(int screenIndex)
    {
        // Get the screen based on the provided screen index
        NSObject screen = GetScreenAtIndex(screenIndex);

        if (screen == null)
            return default;

        CGRect availableScreen;

        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            RectangleF_objc_msgSend_stret(out availableScreen, screen.Handle, VisibleFrameSelector.Handle);
        else
            availableScreen = CGRect_objc_msgSend(screen.Handle, VisibleFrameSelector.Handle);

        if (availableScreen == default)
            return default;

        return availableScreen;
    }

    static Size GetWindowSize(NSObject nsWindow)
    {
        CGRect windowSize;

        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            RectangleF_objc_msgSend_stret(out windowSize, nsWindow.Handle, FrameSelector.Handle);
        else
            windowSize = CGRect_objc_msgSend(nsWindow.Handle, FrameSelector.Handle);

        if (windowSize == default)
            return default;

        return new Size(windowSize.Size.Width, windowSize.Size.Height);
    } 
}