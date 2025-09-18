namespace ParkSpotTLV.App;

public partial class EditCarPage : ContentPage
{
    public EditCarPage()
    {
        InitializeComponent();

        // Load existing car data (for now, populate with sample data)
        LoadCarData();
    }

    private void LoadCarData()
    {
        // TODO: In a real app, you'd pass car ID and load data from database
        // For now, populate with sample data
        CarNameEntry.Text = "My Toyota";
        PrivateRadio.IsChecked = true;
        ResidentPermitCheck.IsChecked = true;
        ZoneNumberEntry.IsVisible = true;
        ZoneNumberEntry.Text = "12";
        DisabledPermitCheck.IsChecked = false;
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

        string message = $"Car updated: {carName} ({carType})";
        if (hasResidentPermit)
            message += $"\nResident permit - Zone: {zoneNumber}";
        if (hasDisabledPermit)
            message += "\nDisabled parking permit";

        await DisplayAlert("Success", message, "OK");

        // Navigate back to previous page
        await Shell.Current.GoToAsync("..");
    }
}