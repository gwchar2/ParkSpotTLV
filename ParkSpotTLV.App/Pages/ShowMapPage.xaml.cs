using ParkSpotTLV.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;
using System.Text.Json;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.App.Data.Services;
using ParkSpotTLV.App.Data.Models;
using System.ComponentModel;
using System.Timers;
using ParkSpotTLV.Core.Models;


namespace ParkSpotTLV.App.Pages;

public partial class ShowMapPage : ContentPage, IDisposable
{
    private bool _disposed = false;
    // Map rendering constants
    private const int MAP_RENDER_DELAY_MS = 500;
    private const int MAP_DEBOUNCE_DELAY_MS = 500;
    private const int DEFAULT_MIN_PARKING_TIME_MINUTES = 30;

    // Map zoom constants
    private const double USER_LOCATION_ZOOM_METERS = 300;
    private const double FALLBACK_ZOOM_KILOMETERS = 1;
    private const double SEARCH_RESULT_ZOOM_METERS = 300;

    // Map area limits to prevent loading too many segments
    private const double MAX_LAT_DEGREES = 0.05;  // ~5.5km
    private const double MAX_LON_DEGREES = 0.05;  // ~5.5km

    private bool isParked = false;
    private string? pickedCarName;
    private string? pickedCarId;
    private int pickedDayOffset = 0; // Days from today (0 = today, 1 = tomorrow, etc.)
    private string? pickedTime;
    DateTimeOffset selectedDate;
    private Guid activePermit;
    private Session? _session; // Single source of truth
    private readonly CarService _carService;
    private readonly MapService _mapService;
    private readonly MapSegmentRenderer _mapSegmentRenderer;
    private readonly ILocalDataService _localDataService;
    private List<Core.Models.Car> _userCars = new();
    private CancellationTokenSource? _mapMoveCts;
    private System.Timers.Timer? _debounceTimer;
    private bool _isInitialized = false;


    public ShowMapPage(CarService carService, MapService mapService, MapSegmentRenderer mapSegmentRenderer, ILocalDataService localDataService)
    {
        InitializeComponent();
        _carService = carService;
        _mapService = mapService;
        _mapSegmentRenderer = mapSegmentRenderer;
        _localDataService = localDataService;

        // Hook up map movement detection
        MyMap.PropertyChanged += MyMapOnPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Only run full initialization once
        if (_isInitialized)
            return;

        try
        {
            await LoadUserCars();
            await LoadSessionPreferences();
            await LoadMapAsync(); // load map, current location

            // Wait for map to render and have a valid VisibleRegion
            await Task.Delay(MAP_RENDER_DELAY_MS);

            var bounds = GetVisibleBounds();
            if (bounds.HasValue)
            {
                await FetchAndRenderSegments(bounds);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("OnAppearing: VisibleRegion not ready, skipping initial segment load");
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to initialize map: {ex.Message}", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Dispose();
    }

    private void MyMapOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion))
        {
            // Debounce: cancel previous timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            // Cancel any in-flight requests
            _mapMoveCts?.Cancel();
            _mapMoveCts = new CancellationTokenSource();

            // Start new timer (debounce)
            _debounceTimer = new System.Timers.Timer(MAP_DEBOUNCE_DELAY_MS);
            _debounceTimer.Elapsed += async (s, args) =>
            {
                _debounceTimer?.Stop();

                // Fetch segments on UI thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var bounds = GetVisibleBounds();
                        if (bounds.HasValue)
                        {
                            await FetchAndRenderSegments(bounds);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in MyMapOnPropertyChanged: {ex.Message}");
                    }
                });
            };
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }
    }

    // calculate bounds of the current map view
    private (double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)?
    GetVisibleBounds()
    {
        if (MyMap == null)
        {
            System.Diagnostics.Debug.WriteLine("GetVisibleBounds: MyMap is null");
            return null;
        }

        var visibleRegion = MyMap.VisibleRegion;

        if (visibleRegion == null)
        {
            System.Diagnostics.Debug.WriteLine("GetVisibleBounds: VisibleRegion is null");
            return null;
        }

        // VisibleRegion gives us the center and radius
        // We need to calculate the bounding box from it
        var center = visibleRegion.Center;
        var latitudeDegrees = visibleRegion.LatitudeDegrees;
        var longitudeDegrees = visibleRegion.LongitudeDegrees;

        // Clamp to maximum area to prevent loading too many segments
        if (latitudeDegrees > MAX_LAT_DEGREES || longitudeDegrees > MAX_LON_DEGREES)
        {
            System.Diagnostics.Debug.WriteLine($"Map area too large ({latitudeDegrees:F4} x {longitudeDegrees:F4}), clamping to center area");
            latitudeDegrees = Math.Min(latitudeDegrees, MAX_LAT_DEGREES);
            longitudeDegrees = Math.Min(longitudeDegrees, MAX_LON_DEGREES);
        }

        // Calculate bounds
        double minLat = center.Latitude - (latitudeDegrees / 2);
        double maxLat = center.Latitude + (latitudeDegrees / 2);
        double minLon = center.Longitude - (longitudeDegrees / 2);
        double maxLon = center.Longitude + (longitudeDegrees / 2);

        return (minLat, maxLat, minLon, maxLon, center.Latitude, center.Longitude);
    }

    private async Task FetchAndRenderSegments((double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)? bounds)
    {
        if (bounds is null)
        {
            System.Diagnostics.Debug.WriteLine("FetchAndRenderSegments: bounds is null");
            return;
        }

        GetMapSegmentsResponse? segmentsResponse;

        try
        {
            System.Diagnostics.Debug.WriteLine($"Fetching segments for bounds: MinLat={bounds.Value.MinLat}, MaxLat={bounds.Value.MaxLat}, MinLon={bounds.Value.MinLon}, MaxLon={bounds.Value.MaxLon}");

            // fetch segments list using getSegmentsAsync(...)
            segmentsResponse = await _mapService.getSegmentsAsync(activePermit,
                                                                    bounds.Value.MinLon,
                                                                    bounds.Value.MinLat,
                                                                    bounds.Value.MaxLon,
                                                                    bounds.Value.MaxLat,
                                                                    bounds.Value.CenterLon,
                                                                    bounds.Value.CenterLat,
                                                                    selectedDate,
                                                                    _session?.MinParkingTime ?? DEFAULT_MIN_PARKING_TIME_MINUTES);

            if (segmentsResponse == null || segmentsResponse.Segments == null)
            {
                System.Diagnostics.Debug.WriteLine("FetchAndRenderSegments: No segments returned from API");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Received {segmentsResponse.Segments.Count} segments");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in FetchAndRenderSegments: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load segments: {ex.Message}", "OK");
            return;
        }

        try
        {
            // Render segments using the renderer
            var renderedCount = _mapSegmentRenderer.RenderSegments(MyMap, segmentsResponse, _session);
            System.Diagnostics.Debug.WriteLine($"Successfully rendered {renderedCount} segments on map");
        }
        catch (OutOfMemoryException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Out of memory rendering segments: {ex.Message}");
            await DisplayAlert("Memory Error", "Too many segments to display. Try zooming in further.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error rendering segments: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to render segments: {ex.Message}", "OK");
        }
    }
    // loads the map, user location and calls the rendering of the segments
    private async Task LoadMapAsync()
    {
        // Enable showing user location on map
        MyMap.IsShowingUser = true;

        // Get user location
        var location = await GetCurrentLocationAsync();

        if (location != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(USER_LOCATION_ZOOM_METERS)));
        }
        else
        {
            // Fallback to Tel Aviv if location unavailable
            var center = new Location(32.0853, 34.7818);
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(FALLBACK_ZOOM_KILOMETERS)));
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
    
    private async Task LoadUserCars()
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

        // Restore previously selected car from session or default to first car
        if (_userCars.Count > 0)
        {
            int selectedIndex = 0;

            // Load session to get last picked car
            var session = await _localDataService.GetSessionAsync();
            if (session != null && !string.IsNullOrEmpty(session.LastPickedCarId))
            {
                int foundIndex = _userCars.FindIndex(c => c.Id == session.LastPickedCarId);
                if (foundIndex >= 0)
                    selectedIndex = foundIndex;
            }

            // Set the picker and update picked car details
            CarPicker.SelectedIndex = selectedIndex;
            pickedCarId = _userCars[selectedIndex].Id;
            pickedCarName = _userCars[selectedIndex].Name;
        }
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

    private void OnDatePickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        pickedDayOffset = picker.SelectedIndex;
    }

    private void OnTimePickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        pickedTime = picker.SelectedItem?.ToString();
    }

    private void OnSettingsToggleClicked(object sender, EventArgs e)
    {
        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;
        String label = "Settings" ;
        if (SettingsPanel.IsVisible)
            label += " ▲";
        else 
            label += " ▼" ;
        SettingsToggleBtn.Text = label;
    }

    // load prefernces from session to UI
    private async Task LoadSessionPreferences()
    {
        _session = await _localDataService.GetSessionAsync();
        if (_session is not null)
        {
            // Update checkbox UI to match session values
            NoParkingCheck.IsChecked = _session.ShowNoParking;
            PaidParkingCheck.IsChecked = _session.ShowPaid;
            FreeParkingCheck.IsChecked = _session.ShowFree;
            RestrictedCheck.IsChecked = _session.ShowRestricted;
        }

        // pickedCarId and pickedCarName are already set by LoadUserCars()
        // Get active permit ID for the selected car
        var activePermitNullable = await permitPopUp();
        activePermit = activePermitNullable ?? Guid.Empty;

        // present day menu according to today
        DateTimeOffset now = DateTimeOffset.Now;
        DatePicker.Items.Clear();
        // Add today through next 7 days
        for (int i = 0; i < 8; i++) {
            DateTimeOffset date = now.AddDays(i);
            string label = date.DayOfWeek.ToString();

            if (i == 0)
                label += " (Today)";
            else if (i == 1)
                label += " (Tomorrow)";

            DatePicker.Items.Add(label);
        }
        DatePicker.SelectedIndex = 0; // Default to today
        pickedDayOffset = 0;

        // set time picker
        TimePicker.Items.Clear();
        for (int i = 0; i < 24; i++)
        {
            string label = $"{i:D2}:00"; // D2 formats as 2-digit (00, 01, 02... 23)
            TimePicker.Items.Add(label);
        }
        TimePicker.SelectedIndex = DateTimeOffset.Now.Hour; // Default to current hour
        pickedTime = TimePicker.SelectedItem?.ToString();
    }

    // save selected preferences to session and reload segments. 
    private async void OnApplyClicked(object sender, EventArgs e)
    {
        // Read current checkbox values (user's changes)
        bool showFree = FreeParkingCheck.IsChecked;
        bool showPaid = PaidParkingCheck.IsChecked;
        bool showRestricted = RestrictedCheck.IsChecked;
        bool showNoParking = NoParkingCheck.IsChecked;

        // update car picked
        int selectedIndex = CarPicker.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _userCars.Count)
        {
            pickedCarId = _userCars[selectedIndex].Id;
            pickedCarName = _userCars[selectedIndex].Name;
        }

        // Save to database
        await _localDataService.UpdatePreferencesAsync(
            showFree: showFree,
            showPaid: showPaid,
            showRestricted: showRestricted,
            showNoParking: showNoParking,
            lastPickedCarId: pickedCarId
        );

        // update date time picked
        selectedDate = GetSelectedDateTime();

        // if car picked has permit options
        var activePermitNullable = await permitPopUp();
        activePermit = activePermitNullable ?? Guid.Empty;


        // Re-fetch segments with updated filter preferences
        var bounds = GetVisibleBounds();
        await FetchAndRenderSegments(bounds);

        await DisplayAlert("Apply", "Changes applied successfully!", "OK");

        // Auto-hide settings panel after applying changes
        SettingsPanel.IsVisible = false;
        SettingsToggleBtn.Text = "Settings ▼";
    }

    private async Task<Guid?> permitPopUp()
    {
        // check if pop up required
        if (pickedCarId is null)
            return null;

        Car? currCar = await _carService.GetCarAsync(pickedCarId);

        if (currCar == null)
            return null;
        
        // If only has resident permit (no disabled permit)
        if (!currCar.HasDisabledPermit) {
            if (currCar.HasResidentPermit)
                return await _carService.getPermitAsync(pickedCarId, 0); // get resident permit
            // No permits - return default/empty permit
            else
                return await _carService.getPermitAsync(pickedCarId, 2); // get default permit
        }

        else { // If car has disabled permit, always ask user if they want to use it
            // Use DisplayActionSheet instead of modal popup to avoid OnAppearing loop
            var alternativeOptionText = "Don't use Disabled Permit";
                

            var action = await DisplayActionSheet(
                $"{currCar.Name} has a disabled permit. Would you like to use it?",
                null, // no cancel button
                null, // no destruction button
                "Use Disabled Permit",
                alternativeOptionText
            );

            if (action == "Use Disabled Permit")
            {
                return await _carService.getPermitAsync(pickedCarId, 1); // disabled permit
            }
            else // alternative option selected
            {
                if (currCar.HasResidentPermit)
                {
                    return await _carService.getPermitAsync(pickedCarId, 0); // resident permit
                }
                else
                {
                    return Guid.Empty; // default permit
                }
            }
        }
        
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
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(SEARCH_RESULT_ZOOM_METERS)));
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

    

    // Helper method to get selected DateTimeOffset
    private DateTimeOffset GetSelectedDateTime()
    {
        DateTimeOffset selectedDate = DateTimeOffset.Now.Date.AddDays(pickedDayOffset);

        // Parse pickedTime (format: "HH:00")
        if (!string.IsNullOrEmpty(pickedTime) && pickedTime.Contains(":"))
        {
            var hourStr = pickedTime.Split(':')[0];
            if (int.TryParse(hourStr, out int hour))
            {
                selectedDate = selectedDate.AddHours(hour);
            }
        }

        return selectedDate;
    }

    // IDisposable implementation
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _debounceTimer = null;

            _mapMoveCts?.Cancel();
            _mapMoveCts?.Dispose();
            _mapMoveCts = null;

            // Unsubscribe from events
            if (MyMap != null)
            {
                MyMap.PropertyChanged -= MyMapOnPropertyChanged;
            }
        }

        _disposed = true;
    }

    // Checkbox change handlers - just for UI feedback
    // No need to store anything, values are read from checkboxes when Apply is clicked
    private void OnNoParkingTapped(object sender, EventArgs e)
    {
        // Checkbox state is automatically tracked by the CheckBox control
    }

    private void OnPaidParkingTapped(object sender, EventArgs e)
    {
        // Checkbox state is automatically tracked by the CheckBox control
    }

    private void OnFreeParkingTapped(object sender, EventArgs e)
    {
        // Checkbox state is automatically tracked by the CheckBox control
    }

    private void OnRestrictedTapped(object sender, EventArgs e)
    {
        // Checkbox state is automatically tracked by the CheckBox control
    }
}