namespace Toolkit;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseToolkit(this MauiAppBuilder builder)
	{
#if MACCATALYST || WINDOWS
        builder.ConfigureMauiHandlers((handlers) =>
        {
            handlers.AddHandler<ModalWindow, ModalWindowHandler>();
#if MACCATALYST
            handlers.AddHandler<MenuBar, ModalMenuBarHandler>();
#endif
        });
#endif

        return builder;
    }
}