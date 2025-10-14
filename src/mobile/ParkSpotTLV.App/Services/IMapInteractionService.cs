namespace ParkSpotTLV.App.Services;

public interface IMapInteractionService : IDisposable
{
    event EventHandler<(double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)>? VisibleBoundsChanged;
    bool IsTrackingUserLocation { get; }
    void Initialize(Microsoft.Maui.Controls.Maps.Map map);
    void MyMapOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e);
    (double MinLat, double MaxLat, double MinLon, double MaxLon, double CenterLat, double CenterLon)? GetVisibleBounds();
    Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync();
    Task<(bool Success, string? ErrorMessage)> SearchAndMoveToAddressAsync(string address, double zoomMeters);
    Task<bool> StartLocationTrackingAsync();
    void StopLocationTracking();
}
