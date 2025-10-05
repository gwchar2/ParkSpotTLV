using ParkSpotTLV.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;
using System.Text.Json;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.App.Data.Services;


namespace ParkSpotTLV.App.Pages;

public partial class ShowMapPage : ContentPage
{
    private bool isParked = false;
    private string? pickedCarName;
    private string? pickedCarId;
    private string? pickedDay;
    private string? pickedTime;
    private bool showRed;
    private bool showBlue;
    private bool showGreen;
    private bool showYellow;
    private readonly CarService _carService;
    private readonly MapService _mapService;
    private readonly ILocalDataService _localDataService;
    private List<Core.Models.Car> _userCars = new();


    public ShowMapPage(CarService carService,MapService mapService,ILocalDataService localDataService )
    {
        InitializeComponent();
        _carService = carService;
        _mapService = mapService;
        _localDataService = localDataService;
        LoadUserCars();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        setSelectedSettings();
        LoadUserCars();
        LoadMapAsync(pickedCarId,pickedDay,pickedTime, showRed , showBlue , showGreen , showYellow);
        
    }


    private async void LoadMapAsync(string? carId, string? day, string? time, bool red, bool blue, bool green, bool yellow)
    {
        // Enable showing user location on map
        MyMap.IsShowingUser = true;

        // Get user location
        var location = await GetCurrentLocationAsync();

        if (location != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));

        }
        else
        {
            // Fallback to Tel Aviv if location unavailable
            var center = new Location(32.0853, 34.7818);
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(5)));
        }

        // Get active permit ID
        var activePermitNullable = await _carService.getActivePermitAsync(carId);
        var activePermit = activePermitNullable ?? Guid.Empty;

        // Get and draw parking segments
        var preferences = await _localDataService.GetUserPreferencesAsync();
        var segmentsResponse = await _mapService.getSegmentsAsync(preferences.ParkingThresholdMinutes, activePermit, null);

        if (segmentsResponse == null || segmentsResponse.Segments == null)
            return;

        // Clear existing map elements
        MyMap.MapElements.Clear();

        // Draw each segment
        foreach (var segment in segmentsResponse.Segments)
        {
            // Determine color based on Group
            Color strokeColor = segment.Group switch
            {
                "FREE" => Color.FromArgb("#40dd7c"),      // Green - Free parking
                "PAID" => Color.FromArgb("#4769b9"),      // Blue - Paid parking
                "LIMITED" => Color.FromArgb("#f2d158"),   // Yellow - Limited/Restricted
                "RESTRICTED" => Color.FromArgb("#f15151"), // Red - No parking
                _ => Color.FromArgb("#808080")            // Gray - Unknown
            };

            // Skip based on filter settings
            if (segment.Group == "RESTRICTED" && !showRed) continue;
            if (segment.Group == "PAID" && !showBlue) continue;
            if (segment.Group == "FREE" && !showGreen) continue;
            if (segment.Group == "LIMITED" && !showYellow) continue;

            // Parse the GeoJSON geometry
            var geometry = segment.Geometry;
            if (geometry.TryGetProperty("type", out var geoType) && geoType.GetString() == "LineString")
            {
                if (geometry.TryGetProperty("coordinates", out var coordinates))
                {
                    var line = new Polyline
                    {
                        StrokeWidth = 6,
                        StrokeColor = strokeColor
                    };

                    foreach (var coordinate in coordinates.EnumerateArray())
                    {
                        // GeoJSON format is [longitude, latitude]
                        double longitude = coordinate[0].GetDouble();
                        double latitude = coordinate[1].GetDouble();
                        line.Geopath.Add(new Location(latitude, longitude));
                    }

                    MyMap.MapElements.Add(line);
                }
            }
        }
    }

    private async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request);
            return location;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Location Error", $"Unable to get location: {ex.Message}", "OK");
            return null;
        }
    }
    private async void LoadUserCars()
    {
        // get user's list of cars from server
        _userCars = await _carService.GetUserCarsAsync();

        // Clear existing items from UI
        CarPicker.Items.Clear();

        // Add user's cars to UI
        foreach (var car in _userCars)
        {
            CarPicker.Items.Add(car.Name);
        }

        // Add "Add Car" option
        if (_userCars.Count < 5)
        {
            CarPicker.Items.Add("+ Add Car");
        }

        // Set default selection to first car if available
        if (_userCars.Count > 0)
        {
            CarPicker.SelectedIndex = 0;
            pickedCarId = _userCars[0].Id;
            pickedCarName = _userCars[0].Name;
        }
    }

    private void OnNoParkingTapped(object sender, EventArgs e)
    {
       showRed = !showRed ;
    }

    private void OnPaidParkingTapped(object sender, EventArgs e)
    {
        showBlue = !showBlue;

    }

    private void OnFreeParkingTapped(object sender, EventArgs e)
    {
        showGreen = !showGreen;
    }

    private void OnRestrictedTapped(object sender, EventArgs e)
    {
       showYellow = !showYellow;
    }

    private async void OnCarPickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var selectedItem = picker.SelectedItem?.ToString();

        if (selectedItem == "+ Add Car")
        {
            await Shell.Current.GoToAsync("AddCarPage");
            // Reset to previous selection after navigation
            _userCars = await _carService.GetUserCarsAsync();
            if (_userCars.Count > 0)
            {
                picker.SelectedIndex = 0; // Back to first car
                pickedCarId = _userCars[0].Id;
                pickedCarName = _userCars[0].Name;
            }
        }
        else
        {
            // Update the selected car ID and name
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _userCars.Count)
            {
                pickedCarId = _userCars[selectedIndex].Id;
                pickedCarName = _userCars[selectedIndex].Name;
            }
        }
    }

    private void OnSettingsToggleClicked(object sender, EventArgs e)
    {
        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;
        SettingsToggleBtn.Text = SettingsPanel.IsVisible ? "⚙️ ▲" : "⚙️ ▼";
    }

    private void setSelectedSettings(){
        // Update car selection
        int selectedIndex = CarPicker.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _userCars.Count)
        {
            pickedCarId = _userCars[selectedIndex].Id;
            pickedCarName = _userCars[selectedIndex].Name;
        }

        pickedDay = DatePicker.SelectedItem?.ToString();
        pickedTime = TimePicker.SelectedItem?.ToString();
        showRed = NoParkingCheck.IsChecked ;
        showBlue  = PaidParkingCheck.IsChecked ;
        showGreen = FreeParkingCheck.IsChecked ;
        showYellow = RestrictedCheck.IsChecked ;
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        setSelectedSettings();
        LoadMapAsync(pickedCarName,pickedDay,pickedTime, showRed , showBlue , showGreen , showYellow);
        await DisplayAlert("Apply", "Changes applied successfully!", "OK");

        // Auto-hide settings panel after applying changes
        SettingsPanel.IsVisible = false;
        SettingsToggleBtn.Text = "⚙️ Settings";
    }

    private async void OnParkHereClicked(object sender, EventArgs e)
    {
        if (!isParked)
        {
            await ShowParkingNotificationPopup();
        }
        else
        {
            // End parking
            isParked = false;
            ParkHereBtn.Text = "Park Here";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#2E7D32");
            await DisplayAlert("Parking Ended", "Your parking session has ended.", "OK");
        }
    }

    private async Task ShowParkingNotificationPopup()
    {
        // Create the popup content
        var popup = new ContentPage
        {
            Title = "Parking Notification"
        };

        var mainLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 30
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Parking Notification",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#2E7D32")
        };

        // Notification text with dropdown
        var notificationLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center
        };

        var notifyLabel1 = new Label
        {
            Text = "Notify me",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        var minutesPicker = new Picker
        {
            Title = "10",
            WidthRequest = 20,
            BackgroundColor = Colors.White,
            SelectedIndex = 1 // Default to 10 minutes
        };
        minutesPicker.Items.Add("5");
        minutesPicker.Items.Add("10");
        minutesPicker.Items.Add("15");
        minutesPicker.Items.Add("20");
        minutesPicker.Items.Add("30");
        minutesPicker.Items.Add("45");
        minutesPicker.Items.Add("60");

        var notifyLabel2 = new Label
        {
            Text = "minutes before parking expires",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        notificationLayout.Children.Add(notifyLabel1);
        notificationLayout.Children.Add(minutesPicker);
        notificationLayout.Children.Add(notifyLabel2);

        // Checkbox
        var checkboxLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center
        };

        var enableCheckbox = new CheckBox
        {
            IsChecked = true
        };

        var checkboxLabel = new Label
        {
            Text = "Enable parking notifications",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        checkboxLayout.Children.Add(enableCheckbox);
        checkboxLayout.Children.Add(checkboxLabel);

        // Buttons
        var buttonLayout = new HorizontalStackLayout
        {
            Spacing = 15,
            HorizontalOptions = LayoutOptions.Center
        };

        var confirmButton = new Button
        {
            Text = "Start Parking",
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            WidthRequest = 120,
            HeightRequest = 45
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#2E7D32"),
            WidthRequest = 100,
            HeightRequest = 45
        };

        confirmButton.Clicked += async (s, e) =>
        {
            string minutes = minutesPicker.SelectedItem?.ToString() ?? "10";
            bool isEnabled = enableCheckbox.IsChecked;

            string message = $"Parking started!";
            if (isEnabled)
                message += $"\nYou'll be notified {minutes} minutes before parking ends.";
            else
                message += "\nNotifications are disabled.";

            // Update button state
            isParked = true;
            ParkHereBtn.Text = "End Parking";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#D32F2F");

            await Navigation.PopModalAsync();

            // Show parking confirmed popup with Pango option
            await ShowParkingConfirmedPopup(message);
        };

        cancelButton.Clicked += async (s, e) =>
        {
            await Navigation.PopModalAsync();
        };

        buttonLayout.Children.Add(confirmButton);
        buttonLayout.Children.Add(cancelButton);

        // Add all elements to main layout
        mainLayout.Children.Add(titleLabel);
        mainLayout.Children.Add(notificationLayout);
        mainLayout.Children.Add(checkboxLayout);
        mainLayout.Children.Add(buttonLayout);

        popup.Content = new ScrollView { Content = mainLayout };

        // Show as modal
        await Navigation.PushModalAsync(popup);
    }

    private async void OnSearchAddress(object sender, EventArgs e)
    {
        var searchBar = (SearchBar)sender;
        var address = searchBar.Text;

        if (string.IsNullOrWhiteSpace(address))
            return;

        try
        {
            var locations = await Geocoding.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
            }
            else
            {
                await DisplayAlert("Not Found", "Address not found. Please try a different search.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Search Error", $"Unable to search: {ex.Message}", "OK");
        }
    }

    private async Task ShowParkingConfirmedPopup(string message)
    {
        // Create the popup content
        var popup = new ContentPage
        {
            Title = "Parking Confirmed"
        };

        var mainLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 30,
            VerticalOptions = LayoutOptions.Center
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Parking Confirmed",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#2E7D32")
        };

        // Message
        var messageLabel = new Label
        {
            Text = message,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Colors.Black
        };

        // Buttons
        var buttonLayout = new HorizontalStackLayout
        {
            Spacing = 15,
            HorizontalOptions = LayoutOptions.Center
        };

        var okButton = new Button
        {
            Text = "OK",
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            WidthRequest = 100,
            HeightRequest = 45,
            CornerRadius = 5
        };

        var pangoButton = new Button
        {
            Text = "Pay with Pango",
            BackgroundColor = Color.FromArgb("#FF6B35"),
            TextColor = Colors.White,
            WidthRequest = 140,
            HeightRequest = 45,
            CornerRadius = 5
        };

        okButton.Clicked += async (s, e) =>
        {
            await Navigation.PopModalAsync();
        };

        pangoButton.Clicked += async (s, e) =>
        {
            // TODO: Navigate to Pango app or show Pango integration
            await DisplayAlert("Pango", "Pango integration coming soon!", "OK");
            await Navigation.PopModalAsync();
        };

        buttonLayout.Children.Add(okButton);
        buttonLayout.Children.Add(pangoButton);

        // Add all elements to main layout
        mainLayout.Children.Add(titleLabel);
        mainLayout.Children.Add(messageLabel);
        mainLayout.Children.Add(buttonLayout);

        popup.Content = new ScrollView { Content = mainLayout };

        // Show as modal
        await Navigation.PushModalAsync(popup);
    }

}