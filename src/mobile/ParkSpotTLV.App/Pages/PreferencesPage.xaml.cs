using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

/*
* Preferences page for configuring app settings.
* Allows users to set minimum parking time preferences.
*/
public partial class PreferencesPage : ContentPage
{
    private readonly ILocalDataService _localDataService;

    /*
    * Initializes the PreferencesPage with required services and loads preferences.
    */
    public PreferencesPage(ILocalDataService localDataService)
    {
        _localDataService = localDataService;
        InitializeComponent();
        LoadPreferencesAsync();
    }

    /*
    * Loads user preferences from local database and updates UI.
    */
    private async void LoadPreferencesAsync()
    {
        try
        {
            var session = await _localDataService.GetSessionAsync();

            var parkingIndex = GetParkingPickerIndex(session?.MinParkingTime ?? 30);
            MinutesPickerParking.SelectedIndex = parkingIndex;
            UpdateParkingExplanation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load preferences: {ex.Message}");
            UpdateParkingExplanation();
        }
    }

    /*
    * Handles minutes picker change. Updates preferences and explanation text.
    */
    private async void OnMinutesPickerChanged(object sender, EventArgs e)
    {
        UpdateParkingExplanation();
        try
        {
            var parkingMinutes = GetMinutesFromPicker(MinutesPickerParking.SelectedItem?.ToString() ?? "30 minutes");

            await _localDataService.UpdatePreferencesAsync(parkingMinutes, null, null, null, null);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"PREFERENCES ERROR: {ex}");
        }
    }

    /*
    * Handles find parking button click. Navigates to ShowMapPage.
    */
    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ShowMapPage");
    }


    /*
    * Updates parking explanation text based on selected minutes.
    */
    private void UpdateParkingExplanation()
    {
        var selectedItem = MinutesPickerParking.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedItem))
        {
            var minutes = selectedItem.Replace(" minutes", "");
            ParkingExplanationLabel.Text = $"parking with less than {minutes} minutes will now be yellow.";
        }
    }

    /*
    * Converts minutes value to picker index.
    */
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

    /*
    * Converts picker text to minutes value.
    */
    private int GetMinutesFromPicker(string pickerText)
    {
        if (string.IsNullOrEmpty(pickerText)) return 30;

        var minutesText = pickerText.Replace(" minutes", "");
        return int.TryParse(minutesText, out var minutes) ? minutes : 30;
    }
}