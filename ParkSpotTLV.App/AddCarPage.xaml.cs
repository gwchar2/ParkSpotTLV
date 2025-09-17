namespace ParkSpotTLV.App;

public partial class AddCarPage : ContentPage
{
    public AddCarPage()
    {
        InitializeComponent();
    }

    private void OnResidentPermitChanged(object sender, CheckedChangedEventArgs e)
    {
        ZoneNumberEntry.IsVisible = e.Value;
    }

    private async void OnSaveCarClicked(object sender, EventArgs e)
    {
        string carName = CarNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(carName))
        {
            await DisplayAlert("Error", "Please enter a car name", "OK");
            return;
        }

        string carType = PrivateRadio.IsChecked ? "Private" : "Truck";
        bool hasResidentPermit = ResidentPermitCheck.IsChecked;
        string zoneNumber = ZoneNumberEntry.Text?.Trim() ?? "";
        bool hasDisabledPermit = DisabledPermitCheck.IsChecked;

        string message = $"Car saved: {carName} ({carType})";
        if (hasResidentPermit)
            message += $"\nResident permit - Zone: {zoneNumber}";
        if (hasDisabledPermit)
            message += "\nDisabled parking permit";

        await DisplayAlert("Success", message, "OK");
        await Shell.Current.GoToAsync("MyCarsPage");
    }
}