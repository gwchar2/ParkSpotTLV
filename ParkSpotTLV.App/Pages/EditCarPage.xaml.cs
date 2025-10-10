using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Models;


namespace ParkSpotTLV.App.Pages;

public partial class EditCarPage : ContentPage, IQueryAttributable
{
    private readonly CarService _carService;
    private string _carId = string.Empty;
    private Car? _currentCar;

    public EditCarPage(CarService carService)
    {
        InitializeComponent();
        _carService = carService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("carId"))
        {
            _carId = query["carId"].ToString() ?? string.Empty;
            LoadCarData();
        }
    }

    private async void LoadCarData()
    {
        if (string.IsNullOrEmpty(_carId))
            return;

        try
        {
            _currentCar = await _carService.GetCarAsync(_carId);

            if (_currentCar == null)
            {
                await DisplayAlert("Error", "Car not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Populate form with car data
            CarNameEntry.Text = _currentCar.Name;
            PrivateRadio.IsChecked = _currentCar.Type == CarType.Private;
            TruckRadio.IsChecked = _currentCar.Type == CarType.Truck;
            ResidentPermitCheck.IsChecked = _currentCar.HasResidentPermit;
            ZoneNumberEntry.IsVisible = _currentCar.HasResidentPermit;
            ZoneNumberEntry.Text = _currentCar.HasResidentPermit ? _currentCar.ResidentPermitNumber.ToString() : "";
            DisabledPermitCheck.IsChecked = _currentCar.HasDisabledPermit;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load car data: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private void OnResidentPermitChanged(object sender, CheckedChangedEventArgs e)
    {
        ZoneNumberEntry.IsVisible = e.Value;
        FreeMinutesEntry.IsVisible = e.Value;
        FreeMinutesLabel.IsVisible = e.Value;
    }

    private async void OnSaveCarClicked(object sender, EventArgs e)
    {
        if (_currentCar == null)
        {
            await DisplayAlert("Error", "No car data loaded", "OK");
            return;
        }

        string carName = CarNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(carName))
        {
            await DisplayAlert("Error", "Please enter a car name", "OK");
            return;
        }

        CarType carType = PrivateRadio.IsChecked ? CarType.Private : CarType.Truck;
        bool hasResidentPermit = ResidentPermitCheck.IsChecked;
        string zoneNumberText = ZoneNumberEntry.Text?.Trim() ?? "";
        bool hasDisabledPermit = DisabledPermitCheck.IsChecked;

        // Validate resident permit number if permit is checked
        int residentPermitNumber = 0;
        if (hasResidentPermit)
        {
            if (string.IsNullOrEmpty(zoneNumberText) || !int.TryParse(zoneNumberText, out residentPermitNumber))
            {
                await DisplayAlert("Error", "Please enter a valid zone number for resident permit", "OK");
                return;
            }
        }

        // Update the current car object
        _currentCar.Name = carName;
        _currentCar.Type = carType;
        _currentCar.HasResidentPermit = hasResidentPermit;
        _currentCar.ResidentPermitNumber = residentPermitNumber;
        _currentCar.HasDisabledPermit = hasDisabledPermit;

        try
        {
            bool success = await _carService.UpdateCarAsync(_currentCar);

            if (success)
            {
                string message = $"Car updated: {carName} ({_currentCar.TypeDisplayName})";
                if (hasResidentPermit)
                    message += $"\nResident permit - Zone: {residentPermitNumber}";
                if (hasDisabledPermit)
                    message += "\nDisabled parking permit";

                await DisplayAlert("Success", message, "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Error", "Failed to update car", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}