using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Models;


namespace ParkSpotTLV.App.Pages;

/*
* Page for adding a new car to the user's account.
* Collects car details including name, type, and permits.
*/
public partial class AddCarPage : ContentPage
{
    private readonly CarService _carService;

    /*
    * Initializes the AddCarPage with required services.
    */
    public AddCarPage(CarService carService)
    {
        InitializeComponent();
        _carService = carService;

    }

    /*
    * Handles resident permit checkbox change. Shows/hides zone number and free minutes fields.
    */
    private void OnResidentPermitChanged(object sender, CheckedChangedEventArgs e)
    {
        ZoneNumberEntry.IsVisible = e.Value;
        FreeMinutesEntry.IsVisible = e.Value;
        FreeMinutesLabel.IsVisible = e.Value;
    }

    /*
    * Handles save car button click. Validates input and saves new car.
    */
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
            Name = carName,
            Type = carType,
            HasResidentPermit = hasResidentPermit,
            ResidentPermitNumber = residentPermitNumber,
            HasDisabledPermit = hasDisabledPermit
        };

        // Save car using CarService
        try
        {
            var addedCar = await _carService.AddCarAsync(newCar);

            if (addedCar == null)
            {
                await DisplayAlert("Error", "Failed to add car", "OK");
                return;
            }

            string message = $"Car saved: {addedCar.Name} ({addedCar.Type})";
            if (addedCar.HasResidentPermit)
                message += $"\nResident permit - Zone: {addedCar.ResidentPermitNumber}";
            if (addedCar.HasDisabledPermit)
                message += "\nDisabled parking permit";

            await DisplayAlert("Success", message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to add car: {ex.Message}");
            await DisplayAlert("Error", "Failed to add car. Please try again.", "OK");
        }
        
    }
}