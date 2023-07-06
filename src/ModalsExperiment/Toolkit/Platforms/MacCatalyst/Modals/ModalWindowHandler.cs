using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Toolkit;

internal sealed class ModalWindowHandler : WindowHandler
{
    bool _modalRunning;
    NSObject _nsWindow;

    protected override UIWindow CreatePlatformElement()
    {
        var uiWindow = base.CreatePlatformElement();

        uiWindow.RestorationIdentifier = null;

        void HandleLoaded(object sender, EventArgs e)
        {
            (VirtualView.Content as Page).Loaded -= HandleLoaded;
            _ = ConfigureModalWindowWhenAddedAsync();
        }

        void HandleAppearing(object sender, EventArgs e)
        {
            (VirtualView.Content as Page).Appearing -= HandleAppearing;
            (VirtualView.Content as Page).Focus();
        }

        void HandleDisappearing(object sender, EventArgs e)
        {
            (VirtualView.Content as Page).Disappearing -= HandleDisappearing;
            UnwindModal();
        }

        (VirtualView.Content as Page).Loaded += HandleLoaded;
        (VirtualView.Content as Page).Appearing += HandleAppearing;
        (VirtualView.Content as Page).Disappearing += HandleDisappearing;

        if (VirtualView.Content is Page page && !string.IsNullOrWhiteSpace(page.Title))
            uiWindow.WindowScene.Title = page.Title;

        return uiWindow;
    }

    async Task ConfigureModalWindowWhenAddedAsync()
    {
        _nsWindow = await PlatformInterop.GetNSWindowWhenAdded();

        if (_nsWindow == null)
            return;

        _modalRunning = true;

        PlatformInterop.ConfigureAsModal(_nsWindow);
        PlatformInterop.RunModal(_nsWindow);
    }

    void UnwindModal()
    {
        if (!_modalRunning)
            return;

        PlatformInterop.StopModal();
        _modalRunning = false;

        _nsWindow?.Dispose();
        _nsWindow = null;

        ModalMenuBarHandler.ConfigureForModal = false;
    }
}