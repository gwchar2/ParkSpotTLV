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

    // Private Load Methods
    private async void LoadPreferencesAsync()
    {
        try
        {
            var session = await _localDataService.GetSessionAsync();

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

    // Event Handlers
    private void OnMinutesPickerChanged(object sender, EventArgs e)
    {
        UpdateParkingExplanation();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            var parkingMinutes = GetMinutesFromPicker(MinutesPickerParking.SelectedItem?.ToString() ?? "30 minutes");

            await _localDataService.UpdatePreferencesAsync(parkingMinutes, null, null, null, null);

            string message = $"Preferences saved!\n\nMinimum parking time: {parkingMinutes} minutes\n";
            await DisplayAlert("Success", message, "OK");

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
        await Shell.Current.GoToAsync("ShowMapPage");
    }

    // Helper Methods
    private void UpdateExplanationTexts()
    {
        UpdateParkingExplanation();
    }

    private void UpdateParkingExplanation()
    {
        var selectedItem = MinutesPickerParking.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedItem))
        {
            var minutes = selectedItem.Replace(" minutes", "");
            ParkingExplanationLabel.Text = $"parking with less than {minutes} minutes will now be yellow.";
        }
    }

    private int GetParkingPickerIndex(int minutes)
    {
        return minutes switch
        {
            10 => 0,
            15 => 1,
            30 => 2,
            45 => 3,
            60 => 4,
            90 => 5,
            120 => 6,
            _ => 2 // Default to 30 minutes
        };
    }

    private int GetMinutesFromPicker(string pickerText)
    {
        if (string.IsNullOrEmpty(pickerText)) return 30;

        var minutesText = pickerText.Replace(" minutes", "");
        return int.TryParse(minutesText, out var minutes) ? minutes : 30;
    }
}