namespace ParkSpotTLV.App;

public partial class ShowMapPage : ContentPage
{
    public ShowMapPage()
    {
        InitializeComponent();
    }

    private void OnNoParkingTapped(object sender, EventArgs e)
    {
       // Filter logic here
    }

    private void OnPaidParkingTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private void OnFreeParkingTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private void OnRestrictedTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private async void OnCarPickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        if (picker.SelectedIndex == 3) // "Add Car" is at index 3
        {
            await Shell.Current.GoToAsync("AddCarPage");
            // Reset to previous selection after navigation
            picker.SelectedIndex = 1; // Back to "Toyota"
        }
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Apply", "Changes applied successfully!", "OK");
    }
}