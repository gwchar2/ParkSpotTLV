namespace ParkSpotTLV.App;

public partial class MyCarsPage : ContentPage
{
    public MyCarsPage()
    {
        InitializeComponent();
    }

    private async void OnAddCarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddCarPage");
    }

    private async void OnShowMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}