namespace Microsoft.Maui.Controls;

public static partial class WindowExtensions
{
    public static Window ResizeAndCenter(this Window window, double width, double height)
        => ResizeAndCenter(window, (int)width, (int)height);

    public static Window ResizeAndCenter(this Window window, int width, int height)
#if !MACCATALYST && !WINDOWS
        => window;
#else
        => window.Resize(width, height).Center();
#endif
}