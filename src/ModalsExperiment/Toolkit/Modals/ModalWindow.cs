namespace Toolkit;

internal sealed class ModalWindow : Window
{
    public ModalWindow() : base() {}
    public ModalWindow(Page page) : base(page) {}

#if MACCATALYST
    internal int TargetScreenIndex { get; set; } = -1;
#elif WINDOWS
    internal int TargetParentWindowIndex { get; set; } = -1;
#endif
}