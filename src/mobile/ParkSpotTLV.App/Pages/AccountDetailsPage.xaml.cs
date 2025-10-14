using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Models;
using Microsoft.Maui.Controls.Shapes;

namespace ParkSpotTLV.App.Pages;

/*
* Account details page for managing user profile and cars.
* Allows users to view/edit account info, manage cars, and change password.
*/
public partial class AccountDetailsPage : ContentPage
{
    private readonly AuthenticationService _authService ;
    private readonly CarService _carService;
    private readonly ParkingService _parkingService;

    /*
    * Initializes the AccountDetailsPage with required services.
    */
    public AccountDetailsPage(CarService carService, AuthenticationService authService,ParkingService parkingService)
    {
        InitializeComponent();
        _carService = carService;
        _authService = authService;
        _parkingService = parkingService;
    }

    /*
    * Called when page appears. Loads user data and cars list.
    */
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserData();
        LoadUserCars();
    }

    /*
    * Loads user data from authentication service and displays username.
    */
    private async void LoadUserData()
    {
        try
        {
            var userMe = await _authService.AuthMeAsync();
            UsernameEntry.Text = userMe.Username;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading user data: {ex.Message}");
        }
    }

    /*
    * Loads user's cars from service and displays them in the UI.
    * Limits maximum cars to 5 and shows appropriate messages.
    */
    private async void LoadUserCars()
    {
        var userCars = await _carService.GetUserCarsAsync();

        // Clear existing cars from UI
        CarsContainer.Children.Clear();

        foreach (var car in userCars)
        {
            await CreateCarUIAsync(car);
        }

        // Show message if no cars
        if (userCars.Count == 0)
        {
            var noCarsLabel = new Label
            {
                Text = "No cars added yet",
                TextColor = Colors.Gray,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            };
            CarsContainer.Children.Add(noCarsLabel);
        }

        if (userCars.Count >= 5)
        {
            AddCarBtn.IsEnabled = false;
            AddCarBtn.Text = "You can have up to 5 cars";
        }
        else
        {
            AddCarBtn.IsEnabled = true;
            AddCarBtn.Text = "Add car";
        }
    }

    /*
    * Handles add car button click. Navigates to AddCarPage.
    */
    private async void OnAddCarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddCarPage");
    }

    /*
    * Handles find parking button click. Navigates to ShowMapPage.
    */
    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ShowMapPage");
    }

    /*
    * Handles car tap gesture. Navigates to EditCarPage with selected car ID.
    */
    private async void OnCarTapped(string carId)
    {
        await Shell.Current.GoToAsync($"EditCarPage?carId={carId}");
    }

    /*
    * Handles remove car button click. Shows confirmation dialog and removes car if confirmed.
    */
    private async void OnRemoveCarClicked(Car car)
    {
        bool confirm = await DisplayAlert("Remove Car",
            $"Are you sure you want to remove car {car.Name}?",
            "Yes", "No");

        if (confirm)
        {
            try
            {
                bool success = await _carService.RemoveCarAsync(car.Id);

                if (success)
                {
                    await DisplayAlert("Success", $"Car {car.Name} has been removed.", "OK");
                    LoadUserCars();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing car: {ex.Message}");
                await DisplayAlert("Error", "Failed to remove car. Please try again.", "OK");
            }
        }
    }

    /*
    * Handles change password button click. Shows password change form.
    */
    private void OnChangePasswordClicked(object sender, EventArgs e)
    {
        PasswordChangeSection.IsVisible = true;
        ChangePasswordBtn.IsVisible = false;

        OldPasswordEntry.Text = "";
        NewPasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
    }

    /*
    * Handles cancel password button click. Hides password change form and clears fields.
    */
    private void OnCancelPasswordClicked(object sender, EventArgs e)
    {
        PasswordChangeSection.IsVisible = false;
        ChangePasswordBtn.IsVisible = true;

        OldPasswordEntry.Text = "";
        NewPasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
    }

    /*
    * Handles save password button click. Validates and updates user password.
    */
    private async void OnSavePasswordClicked(object sender, EventArgs e)
    {
        string oldPassword = OldPasswordEntry.Text?.Trim() ?? "";
        string newPassword = NewPasswordEntry.Text?.Trim() ?? "";
        string confirmPassword = ConfirmPasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(oldPassword))
        {
            await DisplayAlert("Error", "Please enter your current password.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            await DisplayAlert("Error", "Please enter a new password.", "OK");
            return;
        }

        if (newPassword != confirmPassword)
        {
            await DisplayAlert("Error", "New passwords do not match.", "OK");
            return;
        }

        if (!_authService.ValidatePassword(newPassword))
        {
            await DisplayAlert("Error", "Password must be at least 6 characters long and contain no whitespace characters.", "OK");
            return;
        }

        SavePasswordBtn.IsEnabled = false;
        CancelPasswordBtn.IsEnabled = false;
        SavePasswordBtn.Text = "Saving...";

        try
        {
            bool success = await _authService.UpdatePasswordAsync(newPassword, oldPassword);

            if (success)
            {
                await DisplayAlert("Success", "Password updated successfully!", "OK");

                PasswordChangeSection.IsVisible = false;
                ChangePasswordBtn.IsVisible = true;

                OldPasswordEntry.Text = "";
                NewPasswordEntry.Text = "";
                ConfirmPasswordEntry.Text = "";
            }
            else
            {
                await DisplayAlert("Error", "Failed to update password. Please check your current password and try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating password: {ex.Message}");
            await DisplayAlert("Error", "Failed to update password. Please try again later.", "OK");
        }
        finally
        {
            SavePasswordBtn.IsEnabled = true;
            CancelPasswordBtn.IsEnabled = true;
            SavePasswordBtn.Text = "Save";
        }
    }

    /*
    * Creates and adds a car UI element to the cars container.
    * Displays car details, permits, and free parking time remaining.
    */
    private async Task CreateCarUIAsync(Car car)
    {
        var carFrame = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = 15,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnCarTapped(car.Id);
        carFrame.GestureRecognizers.Add(tapGesture);

        var mainLayout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var infoLayout = new VerticalStackLayout { Spacing = 5 };

        var nameLabel = new Label
        {
            Text = $"🚗 {car.Name}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        };

        var typeLabel = new Label
        {
            Text = car.TypeDisplayName,
            FontSize = 14,
            TextColor = Colors.Gray
        };

        infoLayout.Children.Add(nameLabel);
        infoLayout.Children.Add(typeLabel);

        var permitsText = new List<string>();
        if (car.HasResidentPermit)
            permitsText.Add($"Resident #{car.ResidentPermitNumber}");
        if (car.HasDisabledPermit)
            permitsText.Add("Disabled");

        if (permitsText.Any())
        {
            var permitsLabel = new Label
            {
                Text = string.Join(", ", permitsText),
                FontSize = 12,
                TextColor = Colors.Gray
            };
            infoLayout.Children.Add(permitsLabel);
        }

        var FreeParkingText = new List<string>();
        if (car.HasResidentPermit)
        {
            int? budget = await _parkingService.GetParkingBudgetRemainingAsync(Guid.Parse(car.Id));
            FreeParkingText.Add($"Free parking time left: {budget} minutes.");
            var FreeParkingLabel = new Label
            {
                Text = string.Join(", ", FreeParkingText),
                FontSize = 12,
                TextColor = Colors.Gray
            };
            infoLayout.Children.Add(FreeParkingLabel);
        }

        var removeButton = new Button
        {
            Text = "—",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#C42E00"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 28,
            Padding = new Thickness(5, 2),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };
        removeButton.Clicked += (s, e) => OnRemoveCarClicked(car);

        Grid.SetColumn(infoLayout, 0);
        Grid.SetColumn(removeButton, 1);

        mainLayout.Children.Add(infoLayout);
        mainLayout.Children.Add(removeButton);

        carFrame.Content = mainLayout;
        CarsContainer.Children.Add(carFrame);
    }
}