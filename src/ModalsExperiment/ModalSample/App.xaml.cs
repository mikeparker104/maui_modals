namespace ModalSample;

// Configured for multi-window support
// See: https://learn.microsoft.com/dotnet/maui/fundamentals/windows
public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		if (!Current.ModalWindowsSupported())
			MainPage = new MainPage();
	}

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = Current.ModalWindowsSupported() ?
            new Window(new MainPage()) { Title = "Modal Sample" } :
            base.CreateWindow(activationState);

        const int targetWidth = 1920;
        const int targetHeight = 1080;

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var density = displayInfo.Density != 0 ? displayInfo.Density : 1;

        window.ResizeAndCenter(targetWidth / density, targetHeight / density);

        return window;
    }
}