using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.ComponentModel;
using System.Timers;

namespace ParkSpotTLV.App.Services;

// Handles map interaction, location tracking, and viewport management
public class MapInteractionService : IDisposable
{
    private bool _disposed = false;
    private const int MAP_DEBOUNCE_DELAY_MS = 500;
    private const double USER_LOCATION_ZOOM_METERS = 150;
    private const double SEGMENT_RELOAD_DISTANCE_METERS = 150;

    // Map area limits to prevent loading too many segments
    private const double MAX_LAT_DEGREES = 0.05;  // ~5.5km
    private const double MAX_LON_DEGREES = 0.05;  // ~5.5km

    private Microsoft.Maui.Controls.Maps.Map? _map;
    private System.Timers.Timer? _debounceTimer;
    private bool _isTrackingUserLocation = false;
    private Location? _lastSegmentLoadLocation = null;

    public event EventHandler<(double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)>? VisibleBoundsChanged;

    public bool IsTrackingUserLocation => _isTrackingUserLocation;

    // Initializes the service with a map instance and hooks up event handlers
    public void Initialize(Microsoft.Maui.Controls.Maps.Map map)
    {
        if (_map != null)
        {
            // Cleanup previous map if exists
            _map.PropertyChanged -= MyMapOnPropertyChanged;
        }

        _map = map ?? throw new ArgumentNullException(nameof(map));
        _map.PropertyChanged += MyMapOnPropertyChanged;
    }

    // Handles map property changes and triggers debounced viewport updates
    public void MyMapOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion))
        {
            // If tracking user location, check if moved far enough to reload segments
            if (_isTrackingUserLocation)
            {
                var bounds = GetVisibleBounds();
                if (bounds.HasValue)
                {
                    var currentCenter = new Location(bounds.Value.CenterLat, bounds.Value.CenterLon);

                    // Check if moved far enough to reload segments
                    if (_lastSegmentLoadLocation == null ||
                        currentCenter.CalculateDistance(_lastSegmentLoadLocation, DistanceUnits.Kilometers) * 1000 > SEGMENT_RELOAD_DISTANCE_METERS)
                    {
                        _lastSegmentLoadLocation = currentCenter;
                        System.Diagnostics.Debug.WriteLine($"User moved {currentCenter.CalculateDistance(_lastSegmentLoadLocation ?? currentCenter, DistanceUnits.Kilometers) * 1000:F0}m - reloading segments");
                        VisibleBoundsChanged?.Invoke(this, bounds.Value);
                    }
                }
                return; // Still skip debounce timer during tracking
            }

            // Debounce: cancel previous timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            // Start new timer (debounce)
            _debounceTimer = new System.Timers.Timer(MAP_DEBOUNCE_DELAY_MS);
            _debounceTimer.Elapsed += async (s, args) =>
            {
                _debounceTimer?.Stop();

                // Fire event on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        var bounds = GetVisibleBounds();
                        if (bounds.HasValue)
                        {
                            VisibleBoundsChanged?.Invoke(this, bounds.Value);
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

    // Calculates the visible bounds of the current map view
    public (double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)?
        GetVisibleBounds()
    {
        if (_map == null)
        {
            System.Diagnostics.Debug.WriteLine("GetVisibleBounds: Map is null");
            return null;
        }

        var visibleRegion = _map.VisibleRegion;

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

    // Gets the current device location
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request);
            return location;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to get location: {ex.Message}");
            return null;
        }
    }

    // Searches for an address and moves the map to that location
    public async Task<(bool Success, string? ErrorMessage)> SearchAndMoveToAddressAsync(string address, double zoomMeters)
    {
        if (string.IsNullOrWhiteSpace(address))
            return (false, null);

        if (_map == null)
            return (false, "Map not initialized");

        try
        {
            var locations = await Geocoding.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                _map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(zoomMeters)));
                return (true, null);
            }
            else
            {
                return (false, "Address not found. Please try a different search.");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Unable to search: {ex.Message}");
        }
    }

    // Starts tracking user location and following it on the map
    public async Task<bool> StartLocationTrackingAsync()
    {
        if (_isTrackingUserLocation)
            return true; // Already tracking

        try
        {
            _isTrackingUserLocation = true;
            _lastSegmentLoadLocation = null; // Reset segment load tracking
            Geolocation.LocationChanged += OnLocationChanged;

            // Use Best accuracy and smallest distance filter for responsive tracking
            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                MinimumTime = TimeSpan.FromSeconds(1) // Update at most every 1 second
            };
            var result = await Geolocation.StartListeningForegroundAsync(request);

            if (result)
            {
                System.Diagnostics.Debug.WriteLine("Started location tracking successfully");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to start location tracking");
                _isTrackingUserLocation = false;
                Geolocation.LocationChanged -= OnLocationChanged;
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting location tracking: {ex.Message}");
            _isTrackingUserLocation = false;
            Geolocation.LocationChanged -= OnLocationChanged;
            return false;
        }
    }

    // Stops tracking user location
    public void StopLocationTracking()
    {
        if (!_isTrackingUserLocation)
            return; // Not tracking

        _isTrackingUserLocation = false;
        Geolocation.LocationChanged -= OnLocationChanged;
        Geolocation.StopListeningForeground();

        System.Diagnostics.Debug.WriteLine("Stopped location tracking");
    }

    // Handles location changes and updates map position
    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;
        System.Diagnostics.Debug.WriteLine($"Location changed: {location?.Latitude}, {location?.Longitude}");

        if (location != null && _isTrackingUserLocation && _map != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Re-center map on user's new location
                    _map?.MoveToRegion(MapSpan.FromCenterAndRadius(
                        location,
                        Distance.FromMeters(USER_LOCATION_ZOOM_METERS)
                    ));
                    System.Diagnostics.Debug.WriteLine($"Map centered on: {location.Latitude}, {location.Longitude}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error moving map: {ex.Message}");
                }
            });
        }
    }

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
            // Stop location tracking
            StopLocationTracking();

            // Dispose managed resources
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _debounceTimer = null;

            // Unsubscribe from events
            if (_map != null)
            {
                _map.PropertyChanged -= MyMapOnPropertyChanged;
            }
        }

        _disposed = true;
    }
}
