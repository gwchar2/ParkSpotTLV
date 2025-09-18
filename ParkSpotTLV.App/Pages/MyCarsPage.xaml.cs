namespace ParkSpotTLV.App.Pages;

public partial class MyCarsPage : ContentPage
{
    public MyCarsPage()
    {
        InitializeComponent();
    }

    private async void OnCarTapped(object sender, EventArgs e)
    {
        // Navigate to edit car page
        await Shell.Current.GoToAsync("EditCarPage");
    }

    private async void OnRemoveCarClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string carName)
        {
            bool confirm = await DisplayAlert("Remove Car",
                $"Are you sure you want to remove '{carName}'?",
                "Yes", "No");

            if (confirm)
            {
                // TODO: Actually remove the car from the list
                // For now, just show confirmation
                await DisplayAlert("Success", $"'{carName}' has been removed.", "OK");

                // Here you would remove the car from your data source and refresh the UI
                // For now, we'll just show the confirmation
            }
        }
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