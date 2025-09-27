using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class AddCarPage : ContentPage
{
    private readonly CarService _carService; //  = CarService.Instance

    public AddCarPage(CarService carService)
    {
        InitializeComponent();
        _carService = carService;

    }

    private void OnResidentPermitChanged(object sender, CheckedChangedEventArgs e)
    {
        ZoneNumberEntry.IsVisible = e.Value;
    }

    private async void OnSaveCarClicked(object sender, EventArgs e)
    {
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

        // Create new car
        var newCar = new Car
        {
            // Name = carName,
            Type = carType,
            HasResidentPermit = hasResidentPermit,
            ResidentPermitNumber = residentPermitNumber,
            HasDisabledPermit = hasDisabledPermit
        };

        // Save car using CarService
        bool success = await _carService.AddCarAsync(newCar);

        if (success)
        {
            string message = $"Car saved: {carName} ({newCar.TypeDisplayName})";
            if (hasResidentPermit)
                message += $"\nResident permit - Zone: {residentPermitNumber}";
            if (hasDisabledPermit)
                message += "\nDisabled parking permit";

            await DisplayAlert("Success", message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await DisplayAlert("Error", "Failed to add car. You can have maximum 5 cars.", "OK");
        }
    }
}