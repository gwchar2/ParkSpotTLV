using ParkSpotTLV.App.Services;
using Microsoft.Maui.Maps;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Pages;

public partial class ShowMapPage : ContentPage, IDisposable
{
    private bool _disposed = false;
    // Map rendering constants
    private const int MAP_RENDER_DELAY_MS = 500;
    private const int DEFAULT_MIN_PARKING_TIME_MINUTES = 30;

    // Map zoom constants
    private const double USER_LOCATION_ZOOM_METERS = 300;
    private const double FALLBACK_ZOOM_METERS = 300;
    private const double SEARCH_RESULT_ZOOM_METERS = 300;

    private string? pickedCarName;
    private string? pickedCarId;
    private int pickedDayOffset = 0; // Days from today (0 = today, 1 = tomorrow, etc.)
    private string? pickedTime;
    DateTimeOffset selectedDate;
    private Guid activePermit;
    private bool isResidentalPermit;
    private Guid parkingSessionId;
    private Session? _session; // Single source of truth
    private readonly CarService _carService;
    private readonly MapService _mapService;
    private readonly ParkingService _parkingService;
    private readonly MapSegmentRenderer _mapSegmentRenderer;
    private readonly ParkingPopUps _parkingPopUps;
    private readonly LocalDataService _localDataService;
    private readonly MapInteractionService _mapInteractionService;
    private List<Data.Models.Car> _userCars = new();
    private CancellationTokenSource? _mapMoveCts;
    private bool _isInitialized = false;
    private Dictionary<SegmentResponseDTO, string>? segmentsInfo;


    public ShowMapPage(CarService carService, MapService mapService, MapSegmentRenderer mapSegmentRenderer, LocalDataService localDataService, MapInteractionService mapInteractionService, ParkingPopUps parkingPopUps, ParkingService parkingService)
    {
        InitializeComponent();
        _carService = carService;
        _mapService = mapService;
        _mapSegmentRenderer = mapSegmentRenderer;
        _localDataService = localDataService;
        _mapInteractionService = mapInteractionService;
        _parkingService = parkingService;
        _parkingPopUps = parkingPopUps;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Only run full initialization once
        if (_isInitialized)
            return;

        try
        {
            // Initialize MapInteractionService after map is ready
            _mapInteractionService.Initialize(MyMap);
            _mapInteractionService.VisibleBoundsChanged += OnVisibleBoundsChanged;

            await LoadUserCars();
            await LoadSessionPreferences();
            await LoadMapAsync(); // load map, current location

            // Wait for map to render and have a valid VisibleRegion
            await Task.Delay(MAP_RENDER_DELAY_MS);

            var bounds = _mapInteractionService.GetVisibleBounds();
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
        _mapInteractionService.StopLocationTracking();
        Dispose();
    }

    private async void OnVisibleBoundsChanged(object? sender, (double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon) bounds)
    {
        await FetchAndRenderSegments(bounds);
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
            segmentsInfo = _mapSegmentRenderer.RenderSegments(MyMap, segmentsResponse, _session);
            // var uniqueStreets = segmentsInfo.Values.Distinct().ToList();
            // System.Diagnostics.Debug.WriteLine($"Successfully rendered {segmentsInfo.Count} segments on {uniqueStreets.Count} streets: {string.Join(", ", uniqueStreets)}");
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
        var location = await _mapInteractionService.GetCurrentLocationAsync();

        if (location != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(USER_LOCATION_ZOOM_METERS)));
        }
        else
        {
            // Fallback to Tel Aviv if location unavailable
            var center = new Location(32.0853, 34.7818);
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(FALLBACK_ZOOM_METERS)));
        }
    }

    private async void OnTrackLocationToggleClicked(object sender, EventArgs e)
    {
        if (_mapInteractionService.IsTrackingUserLocation)
        {
            _mapInteractionService.StopLocationTracking();
            // Animate toggle to OFF position (left)
            await Task.WhenAll(
                TrackLocationToggleCircle.TranslateTo(0, 0, 200, Easing.CubicInOut),
                TrackLocationToggleBg.FadeTo(1, 200)
            );
            TrackLocationToggleBg.Color = Color.FromArgb("#CCCCCC"); // Gray (off)
        }
        else
        {
            await _mapInteractionService.StartLocationTrackingAsync();
            // Animate toggle to ON position (right)
            await Task.WhenAll(
                TrackLocationToggleCircle.TranslateTo(20, 0, 200, Easing.CubicInOut),
                TrackLocationToggleBg.FadeTo(1, 200)
            );
            TrackLocationToggleBg.Color = Color.FromArgb("#2E7D32"); // Green (on)
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
        (var activePermitNullable,isResidentalPermit) = await _parkingPopUps.ShowPermitPopupAsync(pickedCarId,
            (title, cancel, destruction, buttons) => DisplayActionSheet(title, cancel, destruction, buttons));
        activePermit = activePermitNullable ?? Guid.Empty;

        // set park here button to furrnet car state
        bool isParking = false;
        if (pickedCarId is not null)
        {
            var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(pickedCarId));
            if (response != null)
            {
                isParking = response.Status;
                if (isParking)
                {
                    parkingSessionId = response.SessionId;
                }
            }
        }
        UpdateParkHereButtonState(isParking);

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
        // add check - ative session -> update button parkhere/stopparking
        // set park here button to furrnet car state
        bool isParking = false;
        if (pickedCarId is not null)
        {
            var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(pickedCarId));
            if (response != null)
            {
                isParking = response.Status;
                if (isParking)
                {
                    parkingSessionId = response.SessionId;
                }
            }
        }
        UpdateParkHereButtonState(isParking);


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
        (var activePermitNullable,isResidentalPermit) = await _parkingPopUps.ShowPermitPopupAsync(pickedCarId,
            (title, cancel, destruction, buttons) => DisplayActionSheet(title, cancel, destruction, buttons));
        activePermit = activePermitNullable ?? Guid.Empty;


        // Re-fetch segments with updated filter preferences
        var bounds = _mapInteractionService.GetVisibleBounds();
        await FetchAndRenderSegments(bounds);

        await DisplayAlert("Apply", "Changes applied successfully!", "OK");

        // Auto-hide settings panel after applying changes
        SettingsPanel.IsVisible = false;
        SettingsToggleBtn.Text = "Settings ▼";
    }


    private async void OnParkHereClicked(object sender, EventArgs e)
    {
        // instead of _session.IsParking check -> check Parking status od pickedCarId
        // _session = await _localDataService.GetSessionAsync();
        // set park here button to furrnet car state
        if (string.IsNullOrEmpty(pickedCarId) || _session is null)
            return;

        // get current car session status    
        bool isParking = false;
        var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(pickedCarId));
        if (response != null)
        {
            isParking = response.Status;
            if (isParking)
            {
                parkingSessionId = response.SessionId;
                await DisplayAlert("debug", "parking session is active", "ok");
            }
            await DisplayAlert("debug", "parking session is NOT active", "ok");
        }
    
        bool parkingAtResZone = false;
        StartParkingResponse? startParkingResponse = null;
        
        if (!isParking)
        {
            // let user choose street to park at in order to calculate free parking timer
            if (isResidentalPermit)
            {
                var parkedStreet = await _parkingPopUps.ShowStreetsListPopUpAsync(segmentsInfo, Navigation);
                if (parkedStreet.HasValue)
                {
                    var (parkedStreetName, segmentResponse) = parkedStreet.Value;
                    parkingAtResZone = await parkingAtResidentalZone(segmentResponse);
                    if (!parkingAtResZone)
                    {
                        try
                        {
                            startParkingResponse = await _parkingService.StartParkingAsync(
                                segmentResponse,
                                Guid.Parse(pickedCarId),
                                30, // soon to be removed
                                _session?.MinParkingTime ?? 30);
                            if (startParkingResponse is not null)
                            {
                                parkingSessionId = startParkingResponse.SessionId;
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            await DisplayAlert("Error", $"Failed to start parking: {ex.Message}", "OK");
                            return;
                        }
                    }
                }
                else
                {
                    // User cancelled street selection
                    return;
                }
            }

            
                UpdateParkHereButtonState(true); // update UI button            
                // Show parking confirmed popup with Pango option
                await _parkingPopUps.ShowParkingConfirmedPopupAsync(startParkingResponse,parkingAtResZone, Navigation, DisplayAlert);
        }
        else // currently parking
        {
            // End parking
            if (isResidentalPermit && parkingSessionId != Guid.Empty)
            {
                await DisplayAlert("Debug", "calling stopParkingAsync", "OK");
                try
                {
                    
                    await _parkingService.StopParkingAsync(parkingSessionId, Guid.Parse(pickedCarId));
                }
                catch (HttpRequestException ex)
                {
                    await DisplayAlert("Error", $"Failed to stop parking: {ex.Message}", "OK");
                    return;
                }
            }

            UpdateParkHereButtonState(false); // update UI button
            // await _localDataService.UpdateParkingStatusAsync(false); // update status in session
            await DisplayAlert("Parking Ended", "Your parking session has ended.", "OK");
        }
    }

    // check if the parked street belongs to residenatl zone of current car
    private async Task<bool> parkingAtResidentalZone(SegmentResponseDTO segmentResponse)
    {
        if (pickedCarId is null)
            return false;
        Car? car = await _carService.GetCarAsync(pickedCarId);
        if (car is null)
            return false;
        int zone = car.ResidentPermitNumber;
        if (segmentResponse.ZoneCode == zone)
            return true;
        else
            return false;
    }

    private void UpdateParkHereButtonState(bool isParking)
    {
        if (isParking)
        {
            ParkHereBtn.Text = "End Parking";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#D32F2F");
        }
        else
        {
            ParkHereBtn.Text = "Park Here";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#2E7D32");
        }

    }
    private async void OnSearchAddress(object sender, EventArgs e)
    {
        var searchBar = (SearchBar)sender;
        var address = searchBar.Text;

        var result = await _mapInteractionService.SearchAndMoveToAddressAsync(address, SEARCH_RESULT_ZOOM_METERS);

        if (!result.Success && result.ErrorMessage != null)
        {
            await DisplayAlert(result.ErrorMessage.Contains("not found") ? "Not Found" : "Search Error", result.ErrorMessage, "OK");
        }
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
            _mapMoveCts?.Cancel();
            _mapMoveCts?.Dispose();
            _mapMoveCts = null;

            // Unsubscribe from map interaction service events
            _mapInteractionService.VisibleBoundsChanged -= OnVisibleBoundsChanged;
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