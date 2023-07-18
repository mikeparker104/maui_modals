using Toolkit;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui.Controls;

public static partial class WindowExtensions
{
    public static Window Resize(this Window window, int width, int height)
    {
        window.Width = width;
        window.Height = height;

        return window;
    }

    public static Window Center(this Window window)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            void WindowHandlerChanged(object sender, EventArgs e)
            {
                window.HandlerChanged -= WindowHandlerChanged;

                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                    CenterWindow(window, nativeWindow);
            }

            window.HandlerChanged += WindowHandlerChanged;

            return window;
        }

        CenterWindow(window, nativeWindow);

        return window;
    }

    static void CenterWindow(Window window, Microsoft.UI.Xaml.Window nativeWindow)
    {
        var density = nativeWindow.GetDisplayDensity();

        var screenSize = PlatformInterop.GetAvailableScreenSize(nativeWindow);

        // Center the window
        window.X = (screenSize.Width / density - window.Width) / 2;
        window.Y = (screenSize.Height / density - window.Height) / 2;
    }
}