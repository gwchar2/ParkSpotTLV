namespace ParkSpotTLV.App.Pages;

public partial class PreferencesPage : ContentPage
{
    public PreferencesPage()
    {
        InitializeComponent();
        UpdateExplanationTexts();
    }

    private void OnMinutesPickerChanged(object sender, EventArgs e)
    {
        UpdateParkingExplanation();
    }

    private void OnNotificationMinutesChanged(object sender, EventArgs e)
    {
        UpdateNotificationExplanation();
    }

    private void UpdateExplanationTexts()
    {
        UpdateParkingExplanation();
        UpdateNotificationExplanation();
    }

    private void UpdateParkingExplanation()
    {
        var selectedItem = MinutesPickerParking.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedItem))
        {
            // Extract just the number from "30 minutes"
            var minutes = selectedItem.Replace(" minutes", "");
            ParkingExplanationLabel.Text = $"parking with less than {minutes} minutes will now be yellow.";
        }
    }

    private void UpdateNotificationExplanation()
    {
        var selectedItem = MinutesPickerNotification.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedItem))
        {
            // Extract just the number from "30 minutes"
            var minutes = selectedItem.Replace(" minutes", "");
            NotificationExplanationLabel.Text = $"notify me {minutes} minutes before parking expires.";
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Get current settings
        var parkingMinutes = MinutesPickerParking.SelectedItem?.ToString() ?? "30 minutes";
        var notificationMinutes = MinutesPickerNotification.SelectedItem?.ToString() ?? "30 minutes";
        var notificationsEnabled = NotificationsToggle.IsToggled;

        // Build confirmation message
        string message = $"Preferences saved!\n\n";
        message += $"Minimum parking time: {parkingMinutes}\n";
        message += $"Notifications: {(notificationsEnabled ? "Enabled" : "Disabled")}";

        if (notificationsEnabled)
        {
            message += $"\nNotification time: {notificationMinutes} before expiration";
        }

        await DisplayAlert("Success", message, "OK");

        // Navigate back to previous page
        await Shell.Current.GoToAsync("..");
    }

    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        // Navigate to ShowMapPage
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}