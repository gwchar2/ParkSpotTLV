using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class PreferencesPage : ContentPage
{
    private readonly LocalDataService _localDataService;

    public PreferencesPage(LocalDataService localDataService)
    {
        _localDataService = localDataService;
        InitializeComponent();
        LoadPreferencesAsync();
    }

    private void OnMinutesPickerChanged(object sender, EventArgs e)
    {
        UpdateParkingExplanation();
    }


    private void UpdateExplanationTexts()
    {
        UpdateParkingExplanation();
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

    private async void LoadPreferencesAsync()
    {
        try
        {
            var session = await _localDataService.GetSessionAsync();

            // Set parking threshold picker
            var parkingIndex = GetParkingPickerIndex(session?.MinParkingTime ?? 30);
            MinutesPickerParking.SelectedIndex = parkingIndex;
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
           
            // Save to database
            await _localDataService.UpdatePreferencesAsync(parkingMinutes,null,null,null,null);

            // Build confirmation message
            string message = $"Preferences saved!\n\n";
            message += $"Minimum parking time: {parkingMinutes} minutes\n";

            await DisplayAlert("Success", message, "OK");

            // Navigate back to previous page
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PREFERENCES ERROR: {ex}");
            await DisplayAlert("Error", $"Failed to save preferences:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", "OK");
        }
    }

    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        // Navigate to ShowMapPage
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}