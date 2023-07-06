using Microsoft.Maui.LifecycleEvents;

namespace ModalSample;

public static class AppBuilderExtensions
{
    // Workaround to the reopening of blank windows on launch (MacCatalyst)
    // Occurs if the app is quit with more than 1 window open and the close windows on app close setting is not switched on
    // See: https://github.com/dotnet/maui/issues/10939
    internal static MauiAppBuilder ConfigureMultiWindowStartupBehavior(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if MACCATALYST
            events.AddiOS(ios =>
            {
                ios.FinishedLaunching((a, b) =>
                {
                    // Launch a single window on launch but allow new windows post launch
                    Task.Delay(2000).ContinueWith((task) => { MainThread.BeginInvokeOnMainThread(() => SceneDelegate.FinishedLaunching = true); });
                    return true;
                });
            });
#endif
        });

        return builder;
    }
}