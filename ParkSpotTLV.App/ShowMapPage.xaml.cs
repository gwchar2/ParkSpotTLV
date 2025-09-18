namespace ParkSpotTLV.App;

public partial class ShowMapPage : ContentPage
{
    private bool isParked = false;

    public ShowMapPage()
    {
        InitializeComponent();
    }

    private void OnNoParkingTapped(object sender, EventArgs e)
    {
       // Filter logic here
    }

    private void OnPaidParkingTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private void OnFreeParkingTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private void OnRestrictedTapped(object sender, EventArgs e)
    {
        // Filter logic here
    }

    private async void OnCarPickerChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        if (picker.SelectedIndex == 3) // "Add Car" is at index 3
        {
            await Shell.Current.GoToAsync("AddCarPage");
            // Reset to previous selection after navigation
            picker.SelectedIndex = 1; // Back to "Toyota"
        }
    }

    private void OnSettingsToggleClicked(object sender, EventArgs e)
    {
        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;
        SettingsToggleBtn.Text = SettingsPanel.IsVisible ? "⚙️ ▲" : "⚙️ ▼";
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Apply", "Changes applied successfully!", "OK");

        // Auto-hide settings panel after applying changes
        SettingsPanel.IsVisible = false;
        SettingsToggleBtn.Text = "⚙️ Settings";
    }

    private async void OnParkHereClicked(object sender, EventArgs e)
    {
        if (!isParked)
        {
            await ShowParkingNotificationPopup();
        }
        else
        {
            // End parking
            isParked = false;
            ParkHereBtn.Text = "Park Here";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#2E7D32");
            await DisplayAlert("Parking Ended", "Your parking session has ended.", "OK");
        }
    }

    private async Task ShowParkingNotificationPopup()
    {
        // Create the popup content
        var popup = new ContentPage
        {
            Title = "Parking Notification"
        };

        var mainLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 30
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Parking Notification",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#2E7D32")
        };

        // Notification text with dropdown
        var notificationLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center
        };

        var notifyLabel1 = new Label
        {
            Text = "Notify me",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        var minutesPicker = new Picker
        {
            Title = "10",
            WidthRequest = 20,
            BackgroundColor = Colors.White,
            SelectedIndex = 1 // Default to 10 minutes
        };
        minutesPicker.Items.Add("5");
        minutesPicker.Items.Add("10");
        minutesPicker.Items.Add("15");
        minutesPicker.Items.Add("20");
        minutesPicker.Items.Add("30");
        minutesPicker.Items.Add("45");
        minutesPicker.Items.Add("60");

        var notifyLabel2 = new Label
        {
            Text = "minutes before parking expires",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        notificationLayout.Children.Add(notifyLabel1);
        notificationLayout.Children.Add(minutesPicker);
        notificationLayout.Children.Add(notifyLabel2);

        // Checkbox
        var checkboxLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center
        };

        var enableCheckbox = new CheckBox
        {
            IsChecked = true
        };

        var checkboxLabel = new Label
        {
            Text = "Enable parking notifications",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        checkboxLayout.Children.Add(enableCheckbox);
        checkboxLayout.Children.Add(checkboxLabel);

        // Buttons
        var buttonLayout = new HorizontalStackLayout
        {
            Spacing = 15,
            HorizontalOptions = LayoutOptions.Center
        };

        var confirmButton = new Button
        {
            Text = "Start Parking",
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            WidthRequest = 120,
            HeightRequest = 45
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#2E7D32"),
            WidthRequest = 100,
            HeightRequest = 45
        };

        confirmButton.Clicked += async (s, e) =>
        {
            string minutes = minutesPicker.SelectedItem?.ToString() ?? "10";
            bool isEnabled = enableCheckbox.IsChecked;

            string message = $"Parking started!";
            if (isEnabled)
                message += $"\nYou'll be notified {minutes} minutes before parking ends.";
            else
                message += "\nNotifications are disabled.";

            await DisplayAlert("Parking Confirmed", message, "OK");

            // Update button state
            isParked = true;
            ParkHereBtn.Text = "End Parking";
            ParkHereBtn.BackgroundColor = Color.FromArgb("#D32F2F");

            await Navigation.PopModalAsync();
        };

        cancelButton.Clicked += async (s, e) =>
        {
            await Navigation.PopModalAsync();
        };

        buttonLayout.Children.Add(confirmButton);
        buttonLayout.Children.Add(cancelButton);

        // Add all elements to main layout
        mainLayout.Children.Add(titleLabel);
        mainLayout.Children.Add(notificationLayout);
        mainLayout.Children.Add(checkboxLayout);
        mainLayout.Children.Add(buttonLayout);

        popup.Content = new ScrollView { Content = mainLayout };

        // Show as modal
        await Navigation.PushModalAsync(popup);
    }
    
}