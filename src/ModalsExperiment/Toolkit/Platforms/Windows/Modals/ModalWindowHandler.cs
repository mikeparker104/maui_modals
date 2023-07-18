using Microsoft.Maui.Handlers;

namespace Toolkit;

internal sealed class ModalWindowHandler : WindowHandler
{
    protected override Microsoft.UI.Xaml.Window CreatePlatformElement()
    {
        var window = base.CreatePlatformElement();

        var targetParentWindowIndex = VirtualView is ModalWindow modalWindow ? 
            modalWindow.TargetParentWindowIndex : -1;

        PlatformInterop.ConfigureAsModal(window, targetParentWindowIndex);
        PlatformInterop.RunModal();

        void WindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
        {
            PlatformInterop.StopModal();
            window.Closed -= WindowClosed;
        }

        window.Closed += WindowClosed;

        return window;
    }
}