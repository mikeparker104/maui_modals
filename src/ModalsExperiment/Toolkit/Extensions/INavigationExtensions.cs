using Toolkit;

namespace Microsoft.Maui.Controls;

public static class INavigationExtensions
{
    public static Task PushModalAsyncEx(this INavigation navigation, Page page, Window parentWindow = null)
    {
        var width = page.WidthRequest != -1 ? (int)page.WidthRequest : 800;
        var height = page.HeightRequest != -1 ? (int)page.HeightRequest : 600;

        return PushModalAsyncEx(navigation, page, width, height, parentWindow);
    }

    public static Task PushModalAsyncEx(this INavigation navigation, Page page, double width, double height, Window parentWindow = null)
        => PushModalAsyncEx(navigation, page, (int)width, (int)height, parentWindow);

    public static Task PushModalAsyncEx(this INavigation navigation, Page page, int width, int height, Window parentWindow = null)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException($"Parameters {nameof(width)} and {nameof(height)} must be greater than 0");

#if MACCATALYST

        if (UIKit.UIApplication.SharedApplication.SupportsMultipleScenes)
        {
            ModalMenuBarHandler.ConfigureForModal = true;

            // This is required to get the menu builder code to trigger
            // ===============================================================
            foreach (var window in Application.Current.Windows)
            {
                if (window.Page.MenuBarItems.Any(i => i.Text == ModalMenuBarHandler.MenuBarFileItemText))
                    continue;

                window.Page.MenuBarItems.Add(new MenuBarItem { Text = ModalMenuBarHandler.MenuBarFileItemText });
            }
            // ===============================================================

            var parentWindowScreenIndex = parentWindow?.Handler.PlatformView is UIKit.UIWindow uiWindow ? uiWindow.GetScreenIndex() : -1;

            var modalWindow = new ModalWindow(page)
            {
                MinimumWidth = width,
                MaximumWidth = width,
                MinimumHeight = height,
                MaximumHeight = height,
                TargetScreenIndex = parentWindowScreenIndex
            };

            Application.Current.OpenWindow(modalWindow);

            return Task.CompletedTask;
        }

        System.Diagnostics.Trace.TraceWarning($"Application doesn't support multiple scenes. Falling back to {nameof(INavigation.PushModalAsync)}");

        return navigation.PushModalAsync(page);

#elif WINDOWS

        var appWindowCount = Application.Current.Windows.Count;
        var targetParentWindowIndex = -1;

        for (var i = 0; i < appWindowCount; i++)
        {
            if (Application.Current.Windows[i] == parentWindow)
            {
                targetParentWindowIndex = i;
                break;
            }
        }

        var modalWindow = new ModalWindow(page)
        {
            Title = string.IsNullOrWhiteSpace(page.Title) ? page.GetType().ToString() : page.Title,
            Width = width,
            Height = height,
            TargetParentWindowIndex = targetParentWindowIndex
        };
        
        Application.Current.OpenWindow(modalWindow);

        return Task.CompletedTask;

#else
        return navigation.PushModalAsync(page);
#endif
    }

#pragma warning disable IDE0022
#pragma warning disable IDE0060 // Remove unused parameter
    public static bool ModalWindowsSupported(this INavigation navigation)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0022
        => Application.Current.ModalWindowsSupported();
}