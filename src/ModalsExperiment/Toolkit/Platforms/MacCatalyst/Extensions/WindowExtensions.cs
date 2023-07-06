using Foundation;
using Toolkit;
using UIKit;

namespace Microsoft.Maui.Controls;

public static partial class WindowExtensions
{
    public static Window Resize(this Window window, int width, int height)
    {
        void WindowSizeChanged(object sender, EventArgs e)
        {
            if (window.Height != window.MaximumHeight ||
                window.Width != window.MaximumWidth)
                return;

            window.SizeChanged -= WindowSizeChanged;

            window.MinimumWidth = 0;
            window.MinimumHeight = 0;
            window.MaximumWidth = double.PositiveInfinity;
            window.MaximumHeight = double.PositiveInfinity;
        }

        window.MinimumWidth = width;
        window.MaximumWidth = width;
        window.MinimumHeight = height;
        window.MaximumHeight = height;

        window.SizeChanged += WindowSizeChanged;

        return window;
    }

    public static Window Center(this Window window)
    {
        if (window.Handler?.PlatformView is not UIWindow nativeWindow)
        {
            void WindowHandlerChanged(object sender, EventArgs e)
            {
                window.HandlerChanged -= WindowHandlerChanged;

                if (window.Handler?.PlatformView is UIWindow nativeWindow)
                    CenterWindow(nativeWindow);
            }

            window.HandlerChanged += WindowHandlerChanged;

            return window;
        }

        CenterWindow(nativeWindow);

        return window;
    }

    static void CenterWindow(UIWindow nativeWindow)
    {
        var nsWindow = PlatformInterop.GetNSWindow(nativeWindow);

        if (nsWindow == null)
        {
            _ = CenterWindowWhenAddedAsync();
            return;
        }

        CenterWindow(nsWindow);
    }

    static async Task CenterWindowWhenAddedAsync()
    {
        var nsWindow = await PlatformInterop.GetNSWindowWhenAdded();

        if (nsWindow == null)
        {
            System.Diagnostics.Trace.TraceError("Unable to resolve NSWindow");
            return;
        }

        CenterWindow(nsWindow);
    }

    static void CenterWindow(NSObject nsWindow)
        => PlatformInterop.CenterWindow(nsWindow);
}