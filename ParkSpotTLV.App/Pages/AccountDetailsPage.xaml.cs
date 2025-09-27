using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class AccountDetailsPage : ContentPage
{
    private readonly AuthenticationService _authService ; // = AuthenticationService.Instance
    private readonly CarService _carService ; // = CarService.Instance

    public AccountDetailsPage(CarService carService, AuthenticationService authService)
    {
        InitializeComponent();
        _carService = carService;
        _authService = authService;
        LoadUserData();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserCars();
    }

    private void LoadUserData()
    {
        // Load current user data
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.CurrentUsername))
        {
            UsernameEntry.Text = _authService.CurrentUsername;
        }
        LoadUserCars();
    }

    private async void LoadUserCars()
    {
        var userCars = await _carService.GetUserCarsAsync();

        // Clear existing cars from UI
        CarsContainer.Children.Clear();

        foreach (var car in userCars)
        {
            CreateCarUI(car);
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
    }

    private void CreateCarUI(Car car)
    {
        var carFrame = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeThickness = 1,
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
            Text = $"Car {car.Id}",
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
                TextColor = Color.FromArgb("#2E7D32")
            };
            infoLayout.Children.Add(permitsLabel);
        }

        infoLayout.Children.Add(nameLabel);
        infoLayout.Children.Add(typeLabel);

        var removeButton = new Button
        {
            Text = "Remove",
            BackgroundColor = Color.FromArgb("#D32F2F"),
            TextColor = Colors.White,
            FontSize = 12,
            Padding = new Thickness(8, 4),
            CornerRadius = 5
        };
        removeButton.Clicked += (s, e) => OnRemoveCarClicked(car);

        Grid.SetColumn(infoLayout, 0);
        Grid.SetColumn(removeButton, 1);

        mainLayout.Children.Add(infoLayout);
        mainLayout.Children.Add(removeButton);

        carFrame.Content = mainLayout;
        CarsContainer.Children.Add(carFrame);
    }

    private async void OnEditUsernameClicked(object sender, EventArgs e)
    {
        if (EditUsernameBtn.Text == "Edit")
        {
            // Enable editing
            UsernameEntry.IsReadOnly = false;
            UsernameEntry.BackgroundColor = Colors.White;
            EditUsernameBtn.Text = "Save";
            EditUsernameBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            string newUsername = UsernameEntry.Text?.Trim() ?? "";

            // Validate username
            if (!_authService.ValidateUsername(newUsername))
            {
                await DisplayAlert("Error", "Username must be at least 3 characters and contain only letters, numbers, and underscores.", "OK");
                return;
            }

            // Disable button during update
            EditUsernameBtn.IsEnabled = false;
            EditUsernameBtn.Text = "Saving...";

            try
            {
                bool success = await _authService.UpdateUsernameAsync(newUsername);

                if (success)
                {
                    // Save changes
                    UsernameEntry.IsReadOnly = true;
                    UsernameEntry.BackgroundColor = Colors.LightGray;
                    EditUsernameBtn.Text = "Edit";
                    EditUsernameBtn.BackgroundColor = Color.FromArgb("#2E7D32");

                    await DisplayAlert("Success", "Username updated successfully!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Username already exists. Please choose a different username.", "OK");
                    // Restore original username
                    UsernameEntry.Text = _authService.CurrentUsername;
                }
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "Failed to update username. Please try again later.", "OK");
                // Restore original username
                UsernameEntry.Text = _authService.CurrentUsername;
            }
            finally
            {
                EditUsernameBtn.IsEnabled = true;
                if (EditUsernameBtn.Text == "Saving...")
                {
                    EditUsernameBtn.Text = "Save";
                }
            }
        }
    }

    private async void OnEditPasswordClicked(object sender, EventArgs e)
    {
        if (EditPasswordBtn.Text == "Edit")
        {
            // Enable editing
            PasswordEntry.IsReadOnly = false;
            PasswordEntry.BackgroundColor = Colors.White;
            PasswordEntry.Text = ""; // Clear for new password input
            EditPasswordBtn.Text = "Save";
            EditPasswordBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            string newPassword = PasswordEntry.Text?.Trim() ?? "";

            // Validate password using auth service
            if (!_authService.ValidatePassword(newPassword))
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long and contain no whitespace characters.", "OK");
                return;
            }

            // Disable button during update
            EditPasswordBtn.IsEnabled = false;
            EditPasswordBtn.Text = "Saving...";

            try
            {
                bool success = await _authService.UpdatePasswordAsync(newPassword);

                if (success)
                {
                    // Save changes
                    PasswordEntry.IsReadOnly = true;
                    PasswordEntry.BackgroundColor = Colors.LightGray;
                    PasswordEntry.Text = "••••••••"; // Hide password again
                    EditPasswordBtn.Text = "Edit";
                    EditPasswordBtn.BackgroundColor = Color.FromArgb("#2E7D32");

                    await DisplayAlert("Success", "Password updated successfully!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Failed to update password. Please try again.", "OK");
                    PasswordEntry.Text = "••••••••"; // Reset to hidden password
                }
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "Failed to update password. Please try again later.", "OK");
                PasswordEntry.Text = "••••••••"; // Reset to hidden password
            }
            finally
            {
                EditPasswordBtn.IsEnabled = true;
                if (EditPasswordBtn.Text == "Saving...")
                {
                    EditPasswordBtn.Text = "Save";
                }
            }
        }
    }

    private async void OnCarTapped(string carId)
    {
        // Navigate to edit car page with car ID
        await Shell.Current.GoToAsync($"EditCarPage?carId={carId}");
    }

    private async void OnRemoveCarClicked(Car car)
    {
        bool confirm = await DisplayAlert("Remove Car",
            $"Are you sure you want to remove car {car.Id}?",
            "Yes", "No");

        if (confirm)
        {
            bool success = await _carService.RemoveCarAsync(car.Id);

            if (success)
            {
                await DisplayAlert("Success", $"Car {car.Id} has been removed.", "OK");
                LoadUserCars(); // Refresh the UI
            }
            else
            {
                await DisplayAlert("Error", "Failed to remove car. Please try again.", "OK");
            }
        }
    }

    private async void OnAddCarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddCarPage");
    }

    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        // Navigate to ShowMapPage
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}