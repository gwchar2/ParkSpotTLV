using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class MyCarsPage : ContentPage
{
    private readonly CarService _carService = CarService.Instance;

    public MyCarsPage()
    {
        InitializeComponent();
        LoadUserCars();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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

        // Update add car button visibility (max 5 cars)
        AddCarBtn.IsVisible = userCars.Count < 5;
        if (userCars.Count >= 5)
        {
            var maxCarsLabel = new Label
            {
                Text = "Maximum 5 cars allowed",
                TextColor = Colors.Gray,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10)
            };
            CarsContainer.Children.Add(maxCarsLabel);
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
            Text = car.Name,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        };

        var typeLabel = new Label
        {
            Text = car.TypeDisplayName,
            FontSize = 14,
            TextColor = Colors.Gray
        };

        var permitsLayout = new HorizontalStackLayout { Spacing = 10 };

        if (car.HasResidentPermit)
        {
            permitsLayout.Children.Add(new Label
            {
                Text = $"Resident #{car.ResidentPermitNumber}",
                FontSize = 12,
                TextColor = Color.FromArgb("#2E7D32"),
                BackgroundColor = Color.FromArgb("#E8F5E8"),
                Padding = new Thickness(8, 4)
            });
        }

        if (car.HasDisabledPermit)
        {
            permitsLayout.Children.Add(new Label
            {
                Text = "Disabled",
                FontSize = 12,
                TextColor = Color.FromArgb("#1976D2"),
                BackgroundColor = Color.FromArgb("#E3F2FD"),
                Padding = new Thickness(8, 4)
            });
        }

        infoLayout.Children.Add(nameLabel);
        infoLayout.Children.Add(typeLabel);
        if (permitsLayout.Children.Count > 0)
        {
            infoLayout.Children.Add(permitsLayout);
        }

        var removeButton = new Button
        {
            Text = "Remove",
            BackgroundColor = Color.FromArgb("#D32F2F"),
            TextColor = Colors.White,
            FontSize = 12,
            Padding = new Thickness(10, 5),
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

    private async void OnCarTapped(string carId)
    {
        // Navigate to edit car page with car ID
        await Shell.Current.GoToAsync($"EditCarPage?carId={carId}");
    }

    private async void OnRemoveCarClicked(Car car)
    {
        bool confirm = await DisplayAlert("Remove Car",
            $"Are you sure you want to remove '{car.Name}'?",
            "Yes", "No");

        if (confirm)
        {
            bool success = await _carService.RemoveCarAsync(car.Id);

            if (success)
            {
                await DisplayAlert("Success", $"'{car.Name}' has been removed.", "OK");
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

    private async void OnShowMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}