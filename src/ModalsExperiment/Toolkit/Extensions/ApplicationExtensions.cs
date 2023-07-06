namespace Microsoft.Maui.Controls;

public static class ApplicationExtensions
{
    public static bool ModalWindowsSupported(this Application application)
    {
#if MACCATALYST
        return UIKit.UIApplication.SharedApplication.SupportsMultipleScenes;
#elif WINDOWS
        return true;
#else
        return false;
#endif
    }
}