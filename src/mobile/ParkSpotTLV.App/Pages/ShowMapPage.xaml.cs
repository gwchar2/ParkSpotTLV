using ParkSpotTLV.App.Services;
using Microsoft.Maui.Maps;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Pages;

/*
* Main map page for finding and managing parking spots.
* Displays parking segments on map with color-coded availability and handles parking sessions.
*
* Debouncing Strategy:
* Uses CancellationTokenSource (_mapMoveCts) to debounce map movement and viewport changes.
* When the map bounds change, any pending segment fetch is cancelled and a new one is scheduled
* after MAP_RENDER_DELAY_MS (500ms). This prevents excessive API calls during continuous panning/zooming.
* The MapInteractionService handles the low-level debouncing, while this page cancels/restarts
* the async segment fetching operations.
*
* Dispose Pattern:
* Implements IDisposable to properly clean up resources when the page is closed:
* - Stops location tracking via MapInteractionService
* - Disposes the MapInteractionService itself
* - Cancels any pending map movement debounce operations
* - Unsubscribes from the VisibleBoundsChanged event
* The _disposed flag prevents multiple disposal attempts and ensures OnDisappearing
* only runs cleanup once.
*/
public partial class ShowMapPage : ContentPage, IDisposable
{
    // Tracks whether the page has been disposed
    private bool _disposed = false;
    // Cancellation token for debouncing map movement events
    private CancellationTokenSource? _mapMoveCts;

    // Map constants
    // Delay in milliseconds before rendering map segments after map movement
    private const int MAP_RENDER_DELAY_MS = 500;
    // Default parking duration when user preferences are not set
    private const int DEFAULT_MIN_PARKING_TIME_MINUTES = 30;
    // Zoom level when showing user's current location
    private const double DEFAULT_ZOOM = 300;

    // Tracks whether page has completed initial setup
    private bool _isInitialized = false;

    // Car
    // Name of the Currently selected car 
    private string? __pickedCarName;
    // ID of the currently selected car
    private string? _pickedCarId;
    // Active parking permit ID for the selected car
    private Guid _activePermit;
    // Indicates if the active permit is a residential permit
    private bool _isResidentalPermit;
    // List of user's registered cars
    private List<Data.Models.Car> _userCars = new();

    // Picked day and time
    // Days offset from today (0 = today, 1 = tomorrow, etc.)
    private int _pickedDayOffset = 0;
    // Selected parking start time
    private string? _pickedTime;
    // Combined date and time for parking session
    DateTimeOffset _selectedDate;

    // Sessions
    // Current active parking session ID
    private Guid _parkingSessionId;
    // Current user session with preferences and auth info
    private Session? _session;


    // Services 
    private readonly CarService _carService;
    private readonly MapService _mapService;
    private readonly ParkingService _parkingService;
    private readonly MapSegmentRenderer _mapSegmentRenderer;
    private readonly ParkingPopUps _parkingPopUps;
    private readonly LocalDataService _localDataService;
    private readonly MapInteractionService _mapInteractionService;
   
    // Segments
    // Maps parking segments to their popup detail strings
    private Dictionary<SegmentResponseDTO, string>? _segmentsInfo;
    // Default fallback segment used when no specific segment is selected for parking
    private SegmentResponseDTO _defSegment = new SegmentResponseDTO(
        SegmentId: Guid.Parse("439225be-502b-4832-9c35-2d0342292df1"),
        Tariff: "City_Center",
        ZoneCode: 10,
        NameEnglish: "Weizmann",
        NameHebrew: "ויצמן",
        Group: "PAID",
        Reason: "Will become paid parking at 10/08/2025 10:00:00 +03:00",
        ParkingType: "Paid",
        IsPayNow: false,
        IsPaylater: true,
        AvailableFrom: DateTimeOffset.Now,
        AvailableUntil: DateTimeOffset.Now,
        NextChange: DateTimeOffset.Now,
        FreeBudgetRemaining: 120,
        Geometry: default
    );

    /*
    * Initializes the ShowMapPage with all required services.
    */
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

    /*
    * Called when page appears. Initializes map, loads user data and renders segments.
    */
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsVisible = false
        });

        // Always re-subscribe to event handler when page appears
        _mapInteractionService.VisibleBoundsChanged -= OnVisibleBoundsChanged; // Remove old subscription if exists
        _mapInteractionService.VisibleBoundsChanged += OnVisibleBoundsChanged;

        // Only run full initialization once
        if (_isInitialized)
            return;

        try
        {
            // Initialize MapInteractionService after map is ready
            _mapInteractionService.Initialize(MyMap);

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
            System.Diagnostics.Debug.WriteLine($"Failed to initialize map: {ex.Message}");
            await DisplayAlert("Error", "Failed to load the map. Please try again.", "OK");
        }
    }

    /*
    * Called when page disappears. Stops location tracking and disposes resources.
    */
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _mapInteractionService.StopLocationTracking();
        Dispose();
    }

    /*
    * Handles map visible bounds changes. Fetches and renders segments for new bounds.
    */
    private async void OnVisibleBoundsChanged(object? sender, (double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon) bounds)
    {
        await FetchAndRenderSegments(bounds);
    }

    /*
    * Fetches parking segments from API and renders them on the map.
    * Handles errors and displays appropriate alerts to user.
    */
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

            segmentsResponse = await _mapService.GetSegmentsAsync(_activePermit,
                                                                   bounds.Value.MinLon,
                                                                   bounds.Value.MinLat,
                                                                   bounds.Value.MaxLon,
                                                                   bounds.Value.MaxLat,
                                                                   bounds.Value.CenterLon,
                                                                   bounds.Value.CenterLat,
                                                                   _selectedDate,
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
            System.Diagnostics.Debug.WriteLine($"Failed to load segments: {ex.Message}");
            await DisplayAlert("Error", "Unable to load parking data. Please check your connection.", "OK");
            return;
        }

        try
        {
            // Render segments using the renderer
            _segmentsInfo = _mapSegmentRenderer.RenderSegments(MyMap, segmentsResponse, _session);
            // var uniqueStreets = _segmentsInfo.Values.Distinct().ToList();
            // System.Diagnostics.Debug.WriteLine($"Successfully rendered {_segmentsInfo.Count} segments on {uniqueStreets.Count} streets: {string.Join(", ", uniqueStreets)}");
        }
        catch (OutOfMemoryException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Memory error rendering segments: {ex.Message}");
            await DisplayAlert("Memory Error", "Too many segments to display. Try zooming in further.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to render segments: {ex.Message}");
            await DisplayAlert("Error", "Unable to display parking segments. Please try again.", "OK");
        }
    }

    /*
    * Loads the map, gets user location and centers map view.
    */
    private async Task LoadMapAsync()
    {
        // Enable showing user location on map
        MyMap.IsShowingUser = true;

        // Get user location
        var location = await _mapInteractionService.GetCurrentLocationAsync();

        if (location != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(DEFAULT_ZOOM)));
        }
        else
        {
            // Fallback to Tel Aviv if location unavailable
            var center = new Location(32.0853, 34.7818);
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(DEFAULT_ZOOM)));
        }
    }

    /*
    * Handles location tracking toggle click. Starts or stops tracking user location with animation.
    */
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
            TrackLocationToggleBg.Color = Color.FromArgb("#FF2B3271"); // Green (on)
        }
    }

    /*
    * Loads user's cars from service and populates car picker.
    */
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
            _pickedCarId = _userCars[selectedIndex].Id;
            _pickedCarName = _userCars[selectedIndex].Name;

        }
    }

    /*
    * Handles car picker selection change. Navigates to add car page or updates selected car.
    */
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
                _pickedCarId = _userCars[0].Id;
                _pickedCarName = _userCars[0].Name;
            }
        }
        else
        {
            // Update the selected car ID and name
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _userCars.Count)
            {
                _pickedCarId = _userCars[selectedIndex].Id;
                _pickedCarName = _userCars[selectedIndex].Name;
            }
        }

    }

    /*
    * Handles date picker change. Updates picked day offset.
    */
    private void OnDatePickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        _pickedDayOffset = picker.SelectedIndex;
    }

    /*
    * Handles time picker change. Updates picked time.
    */
    private void OnTimePickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        _pickedTime = picker.SelectedItem?.ToString();
    }

    /*
    * Handles settings toggle button click. Shows/hides settings panel.
    */
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

    /*
    * Loads user preferences from session and initializes UI controls.
    * Sets up parking filters, date/time pickers, and car selection.
    */
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

        // _pickedCarId and _pickedCarName are already set by LoadUserCars()
        // Get active permit ID for the selected car
        (var _activePermitNullable,_isResidentalPermit) = await _parkingPopUps.ShowPermitPopupAsync(_pickedCarId,
            (title, cancel, destruction, buttons) => DisplayActionSheet(title, cancel, destruction, buttons));
        _activePermit = _activePermitNullable ?? Guid.Empty;

        // set park here button to furrnet car state
        bool isParking = false;
        if (_pickedCarId is not null)
        {
            var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(_pickedCarId));
            if (response != null)
            {
                isParking = response.Status;
                if (isParking)
                {
                    _parkingSessionId = response.SessionId;
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
        _pickedDayOffset = 0;

        // set time picker
        TimePicker.Items.Clear();
        for (int i = 0; i < 24; i++)
        {
            string label = $"{i:D2}:00"; // D2 formats as 2-digit (00, 01, 02... 23)
            TimePicker.Items.Add(label);
        }
        TimePicker.SelectedIndex = DateTimeOffset.Now.Hour; // Default to current hour
        _pickedTime = TimePicker.SelectedItem?.ToString();
    }

    /*
    * Handles apply button click. Saves preferences and reloads parking segments.
    */
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
            _pickedCarId = _userCars[selectedIndex].Id;
            _pickedCarName = _userCars[selectedIndex].Name;
        }
        // add check - ative session -> update button parkhere/stopparking
        // set park here button to furrnet car state
        bool isParking = false;
        if (_pickedCarId is not null)
        {
            var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(_pickedCarId));
            if (response != null)
            {
                isParking = response.Status;
                if (isParking)
                {
                    _parkingSessionId = response.SessionId;
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
            lastPickedCarId: _pickedCarId
        );

        _session = await _localDataService.GetSessionAsync();

        // update date time picked
        _selectedDate = GetSelectedDateTime();

        // if car picked has permit options
        (var _activePermitNullable,_isResidentalPermit) = await _parkingPopUps.ShowPermitPopupAsync(_pickedCarId,
            (title, cancel, destruction, buttons) => DisplayActionSheet(title, cancel, destruction, buttons));
        _activePermit = _activePermitNullable ?? Guid.Empty;


        // Re-fetch segments with updated filter preferences
        var bounds = _mapInteractionService.GetVisibleBounds();
        await FetchAndRenderSegments(bounds);

        // Auto-hide settings panel after applying changes
        SettingsPanel.IsVisible = false;
        SettingsToggleBtn.Text = "Settings ▼";
    }

    /*
    * Handles park here button click. Starts or ends parking session based on current state.
    * Manages segment selection, residential permits, and parking confirmation.
    */
    private async void OnParkHereClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pickedCarId) || _session is null)
            return;

        // get current car session status    
        bool isParking = false;
        var response = await _parkingService.GetParkingStatusAsync(Guid.Parse(_pickedCarId));
        if (response != null)
        {
            isParking = response.Status;
            if (isParking)
            {
                _parkingSessionId = response.SessionId;
            }
        }
    
        bool parkingAtResZone = false;
        StartParkingResponse? startParkingResponse = null;
        SegmentResponseDTO segmentToUse = _defSegment;

        // Re-fetch segments for updated time -> _segmentsInfo updated in FetachAndRender
        var bounds = _mapInteractionService.GetVisibleBounds();
        await FetchAndRenderSegments(bounds);

        if (!isParking)
        {
            // let user choose street to park at in order to calculate free parking timer
            if (_isResidentalPermit){
                var parkedStreet = await _parkingPopUps.ShowStreetsListPopUpAsync(_segmentsInfo, Navigation);
                if (parkedStreet.HasValue) {
                    var (parkedStreetName, segmentResponse) = parkedStreet.Value;
                    segmentToUse = segmentResponse;
                    parkingAtResZone = await parkingAtResidentalZone(segmentResponse);
                }
            }
            else {
                // For non-residential permits, use the first segment from the list
                if (_segmentsInfo != null && _segmentsInfo.Count > 0) {
                    segmentToUse = _segmentsInfo.First().Key;
                }
            }

            try
            {
                startParkingResponse = await _parkingService.StartParkingAsync(
                    segmentToUse,
                    Guid.Parse(_pickedCarId),
                    120); //_session?.MinParkingTime ?? 
                if (startParkingResponse is not null)
                {
                    _parkingSessionId = startParkingResponse.SessionId;
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start parking: {ex.Message}");
                await DisplayAlert("Error", "Unable to start parking. Please check your connection.", "OK");
                return;
            }

            // if has residential permit and parking out of zone - show free minutes left today
            if (_isResidentalPermit && !parkingAtResZone) {
                int? budget = await _parkingService.GetParkingBudgetRemainingAsync(Guid.Parse(_pickedCarId));
                await _parkingPopUps.ShowParkingConfirmedPopupAsync(budget ?? 0, Navigation, DisplayAlert);
            }
            UpdateParkHereButtonState(true); // update UI button - this will also show the Pango button
        }
        else // currently parking
        {
            // End parking
            if (_isResidentalPermit && _parkingSessionId != Guid.Empty)
            {
                try
                {
                    await _parkingService.StopParkingAsync(_parkingSessionId, Guid.Parse(_pickedCarId));
                    int? budget = await _parkingService.GetParkingBudgetRemainingAsync(Guid.Parse(_pickedCarId));
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to stop parking: {ex.Message}");
                    await DisplayAlert("Error", "Unable to stop parking. Please check your connection.", "OK");
                    return;
                }
            }

            UpdateParkHereButtonState(false); // update UI button
        }
    }

    /*
    * Checks if the parked street belongs to the car's residential zone.
    */
    private async Task<bool> parkingAtResidentalZone(SegmentResponseDTO segmentResponse)
    {
        if (_pickedCarId is null)
            return false;
        Car? car = await _carService.GetCarAsync(_pickedCarId);
        if (car is null)
            return false;
        int zone = car.ResidentPermitNumber;
        if (segmentResponse.ZoneCode == zone)
            return true;
        else
            return false;
    }

    /*
    * Updates park here button text and color based on parking state.
    */
    private void UpdateParkHereButtonState(bool isParking)
    {
        if (isParking)
        {
            ParkHereBtn.Text = "End Parking";
            ParkHereBtn.BorderColor = Color.FromArgb("#FFF15151");
            ParkHereBtn.TextColor = Color.FromArgb("#FFF15151");
            PayWithPangoBtn.IsVisible = true; // Show Pango button when parking
        }
        else
        {
            ParkHereBtn.Text = "Park Here";
            ParkHereBtn.BorderColor = Color.FromArgb("#FF2B3271");
            ParkHereBtn.TextColor = Color.FromArgb("#FF2B3271");
            PayWithPangoBtn.IsVisible = false; // Hide Pango button when not parking
        }

    }

    /*
    * Handles Pango payment button click. Opens Pango app or redirects to app store.
    */
    private async void OnPayWithPangoClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync("pango://");
        }
        catch
        {
            // Fallback to app store
            if (DeviceInfo.Platform == DevicePlatform.iOS)
                await Launcher.OpenAsync("https://apps.apple.com/app/pango");
            else
                await Launcher.OpenAsync("https://play.google.com/store/apps/details?id=com.pango.android");
        }
    }

    /*
    * Handles address search. Searches for address and moves map to location.
    */
    private async void OnSearchAddress(object sender, EventArgs e)
    {
        var searchBar = (SearchBar)sender;
        var address = searchBar.Text;

        var result = await _mapInteractionService.SearchAndMoveToAddressAsync(address, DEFAULT_ZOOM);

        // Re-fetch segments with updated filter preferences
        var bounds = _mapInteractionService.GetVisibleBounds();
        await FetchAndRenderSegments(bounds);
        
        if (!result.Success && result.ErrorMessage != null)
        {
            System.Diagnostics.Debug.WriteLine($"Address search failed: {result.ErrorMessage}");
            await DisplayAlert("Search Error", "Unable to find address. Please try a different search.", "OK");
        }
    }

    /*
    * Combines selected date and time into a DateTimeOffset.
    * Returns the combined date and time for parking session.
    */
    private DateTimeOffset GetSelectedDateTime()
    {
        DateTimeOffset _selectedDate = DateTimeOffset.Now.Date.AddDays(_pickedDayOffset);

        // Parse _pickedTime (format: "HH:00")
        if (!string.IsNullOrEmpty(_pickedTime) && _pickedTime.Contains(":"))
        {
            var hourStr = _pickedTime.Split(':')[0];
            if (int.TryParse(hourStr, out int hour))
            {
                _selectedDate = _selectedDate.AddHours(hour);
            }
        }

        return _selectedDate;
    }

    /*
    * Disposes resources used by the page.
    */
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /*
    * Protected dispose method for releasing managed and unmanaged resources.
    */
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


}