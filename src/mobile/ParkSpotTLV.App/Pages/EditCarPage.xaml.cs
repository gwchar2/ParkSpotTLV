using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Models;


namespace ParkSpotTLV.App.Pages;

/*
* Page for editing an existing car's details.
* Allows updating car name, type, and permit information.
*/
public partial class EditCarPage : ContentPage, IQueryAttributable
{
    private readonly ICarService _carService;
    private readonly IParkingService _parkingService;
    private string _carId = string.Empty;
    private Car? _currentCar;

    /*
    * Initializes the EditCarPage with required services.
    */
    public EditCarPage(ICarService carService, IParkingService parkingService)
    {
        InitializeComponent();
        _carService = carService;
        _parkingService = parkingService;
    }


    /*
    * Applies query attributes from navigation. Extracts car ID and loads car data.
    */
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("carId"))
        {
            _carId = query["carId"].ToString() ?? string.Empty;
            LoadCarData();
        }
    }

    /*
    * Loads car data from service and populates form fields.
    */
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
            System.Diagnostics.Debug.WriteLine($"Failed to load car data: {ex.Message}");
            await DisplayAlert("Error", "Failed to load car data. Please try again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    /*
    * Handles resident permit checkbox change. Shows/hides zone number field.
    */
    private void OnResidentPermitChanged(object sender, CheckedChangedEventArgs e)
    {
        ZoneNumberEntry.IsVisible = e.Value;
    }

    /*
    * Handles save car button click. Validates input and updates car details.
    */
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
            System.Diagnostics.Debug.WriteLine($"Failed to update car: {ex.Message}");
            await DisplayAlert("Error", "Failed to update car. Please try again.", "OK");
        }
    }
}