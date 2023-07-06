using Foundation;

namespace ModalSample;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    internal static bool FinishedLaunching { get; set; }

    public override void WillConnect(UIKit.UIScene scene, UIKit.UISceneSession session, UIKit.UISceneConnectionOptions connectionOptions)
    {
        base.WillConnect(scene, session, connectionOptions);

        if (FinishedLaunching)
            return;

        // Workaround to the reopening of blank windows on launch (MacCatalyst)
        // Occurs if the app is quit with more than 1 window open and the close windows on app close setting is not switched on
        // See: https://github.com/dotnet/maui/issues/10939
        if (Application.Current.Windows.Count > 1)
            Application.Current.CloseWindow(Application.Current.Windows[Application.Current.Windows.Count - 1]);
    }
}