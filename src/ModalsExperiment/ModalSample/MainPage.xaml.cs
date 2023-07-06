namespace ModalSample;

public partial class MainPage : ContentPage
{

	public MainPage()
	{
		InitializeComponent();
	}

    void ShowModalButtonClicked(object sender, EventArgs e)
        => _ = Navigation.PushModalAsyncEx(new ModalPage(), Window.Width / 2, Window.Height / 2);
}