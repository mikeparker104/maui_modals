using Microsoft.Maui.Handlers;
using UIKit;

namespace Toolkit;

internal sealed class ModalMenuBarHandler : MenuBarHandler
{
    internal const string MenuBarFileItemText = "File";

    internal static bool ConfigureForModal { get; set; }

    protected override void ConnectHandler(IUIMenuBuilder platformView)
    {
        base.ConnectHandler(platformView);

        // Remove custom File item if not configuring for modal
        if (!ConfigureForModal)
        {
            foreach (var window in Application.Current.Windows)
            {
                if (!window.Page.MenuBarItems.Any(i => i.Text == MenuBarFileItemText))
                    continue;

                window.Page.MenuBarItems.Remove(window.Page.MenuBarItems.First(i => i.Text == MenuBarFileItemText));
            }
        }

        if (ConfigureForModal)
            ConfigureMenuForModal(platformView);
    }

    static void ConfigureMenuForModal(IUIMenuBuilder menuBuilder)
    {
        var newSceneMenu = menuBuilder.GetMenu(UIMenuIdentifier.NewScene.GetConstant());

        if (newSceneMenu != null)
            menuBuilder.RemoveMenu(UIMenuIdentifier.NewScene.GetConstant());
    }
}