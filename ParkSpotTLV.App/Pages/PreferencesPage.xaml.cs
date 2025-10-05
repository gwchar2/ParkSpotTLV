using ParkSpotTLV.App.Data.Services;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.App.Pages;

public partial class PreferencesPage : ContentPage
{
    private readonly ILocalDataService _localDataService;

    public PreferencesPage(ILocalDataService localDataService)
    {
        _localDataService = localDataService;
        InitializeComponent();
        LoadPreferencesAsync();
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

    private async void LoadPreferencesAsync()
    {
        try
        {
            var preferences = await _localDataService.GetUserPreferencesAsync();

            // Set parking threshold picker
            var parkingIndex = GetParkingPickerIndex(preferences.ParkingThresholdMinutes);
            MinutesPickerParking.SelectedIndex = parkingIndex;

            // Set notification picker
            var notificationIndex = GetNotificationPickerIndex(preferences.NotificationMinutesBefore);
            Console.WriteLine($"DEBUG: NotificationMinutesBefore = {preferences.NotificationMinutesBefore}, calculated index = {notificationIndex}");
            MinutesPickerNotification.SelectedIndex = notificationIndex;

            // Set notifications toggle
            NotificationsToggle.IsToggled = preferences.NotificationsEnabled;

            UpdateExplanationTexts();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load preferences: {ex.Message}");
            UpdateExplanationTexts();
        }
    }

    private int GetParkingPickerIndex(int minutes)
    {
        // Parking picker items: 10, 15, 30, 45, 60, 90, 120 minutes
        return minutes switch
        {
            10 => 0,
            15 => 1,
            30 => 2,
            45 => 3,
            60 => 4,
            90 => 5,
            120 => 6,
            _ => 2 // Default to 30 minutes (index 2)
        };
    }

    private int GetNotificationPickerIndex(int minutes)
    {
        // Notification picker items: 5, 10, 15, 30, 45, 60 minutes
        return minutes switch
        {
            5 => 0,
            10 => 1,
            15 => 2,
            30 => 3,
            45 => 4,
            60 => 5,
            _ => 3 // Default to 30 minutes (index 3)
        };
    }

    private int GetMinutesFromPicker(string pickerText)
    {
        if (string.IsNullOrEmpty(pickerText)) return 30;

        var minutesText = pickerText.Replace(" minutes", "");
        return int.TryParse(minutesText, out var minutes) ? minutes : 30;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Get current settings
            var parkingMinutes = GetMinutesFromPicker(MinutesPickerParking.SelectedItem?.ToString() ?? "30 minutes");
            var notificationMinutes = GetMinutesFromPicker(MinutesPickerNotification.SelectedItem?.ToString() ?? "30 minutes");
            var notificationsEnabled = NotificationsToggle.IsToggled;

            // Save to database
            var preferences = await _localDataService.GetUserPreferencesAsync();
            preferences.ParkingThresholdMinutes = parkingMinutes;
            preferences.NotificationMinutesBefore = notificationMinutes;
            preferences.NotificationsEnabled = notificationsEnabled;
            preferences.LastUpdated = DateTime.UtcNow;

            await _localDataService.SaveUserPreferencesAsync(preferences);

            // Build confirmation message
            string message = $"Preferences saved!\n\n";
            message += $"Minimum parking time: {parkingMinutes} minutes\n";
            message += $"Notifications: {(notificationsEnabled ? "Enabled" : "Disabled")}";

            if (notificationsEnabled)
            {
                message += $"\nNotification time: {notificationMinutes} minutes before expiration";
            }

            await DisplayAlert("Success", message, "OK");

            // Navigate back to previous page
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PREFERENCES ERROR: {ex}");
            System.Diagnostics.Debug.WriteLine($"PREFERENCES ERROR: {ex}");
            await DisplayAlert("Error", $"Failed to save preferences:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", "OK");
        }
    }

    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        // Navigate to ShowMapPage
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}