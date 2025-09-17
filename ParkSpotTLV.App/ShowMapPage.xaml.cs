namespace ParkSpotTLV.App;

public partial class ShowMapPage : ContentPage
{
    public ShowMapPage()
    {
        InitializeComponent();
    }

    private async void OnNoParkingTapped(object sender, EventArgs e)
    {
       // await DisplayAlert("Filter", "Showing No Parking areas", "OK");
    }

    private async void OnPaidParkingTapped(object sender, EventArgs e)
    {
        //await DisplayAlert("Filter", "Showing Paid Parking areas", "OK");
    }

    private async void OnFreeParkingTapped(object sender, EventArgs e)
    {
        //await DisplayAlert("Filter", "Showing Free Parking areas", "OK");
    }

    private async void OnRestrictedTapped(object sender, EventArgs e)
    {
        //await DisplayAlert("Filter", "Showing Restricted areas", "OK");
    }
}