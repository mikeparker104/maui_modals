namespace ModalSample;

public partial class ModalPage : ContentPage
{
	public ModalPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var modalWindowsSupported = Navigation.ModalWindowsSupported();

        CloseButton.IsVisible = !modalWindowsSupported;

        PresentationLabel.Text =
            $"This has been presented {(modalWindowsSupported ?
            "as a separate modal window" :
            "within the bounds of the current window")}";
    }

    void CloseButtonClicked(object sender, EventArgs e)
        => _ = Navigation.PopModalAsync();
}