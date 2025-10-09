using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.App.Services;

// Handles parking-related UI operations including popups and permits
public class ParkingPopUps
{
    private readonly CarService _carService;

    public ParkingPopUps(CarService carService)
    {
        _carService = carService ?? throw new ArgumentNullException(nameof(carService));
    }

    // Shows permit selection dialog and returns the selected permit ID
    public async Task<(Guid? activePermitId, bool isResidential)> ShowPermitPopupAsync(string? pickedCarId, Func<string, string?, string?, string[], Task<string>> displayActionSheet)
    {
        // check if pop up required
        if (pickedCarId is null)
            return (null,false);

        Car? currCar = await _carService.GetCarAsync(pickedCarId);

        if (currCar == null)
            return (null,false);

        // If only has resident permit (no disabled permit)
        if (!currCar.HasDisabledPermit)
        {
            if (currCar.HasResidentPermit)
                return (await _carService.GetPermitAsync(pickedCarId, 0),true); // get resident permit
            // No permits - return default/empty permit
            else
                return (await _carService.GetPermitAsync(pickedCarId, 2),false); // get default permit
        }

        else
        { // If car has disabled permit, always ask user if they want to use it
            // Use DisplayActionSheet instead of modal popup to avoid OnAppearing loop
            var action = await displayActionSheet(
                $"{currCar.Name} has a disabled permit. Would you like to use it?",
                null, // no cancel button
                null, // no destruction button
                new[] { "Use Disabled Permit", "Don't use Disabled Permit" }
            );

            if (action == "Use Disabled Permit")
            {
                return (await _carService.GetPermitAsync(pickedCarId, 1),false); // disabled permit
            }
            else // alternative option selected
            {
                if (currCar.HasResidentPermit)
                {
                    return (await _carService.GetPermitAsync(pickedCarId, 0),true); // resident permit
                }
                else
                {
                    return (Guid.Empty,false); // default permit
                }
            }
        }
    }

    // Shows parking notification popup and returns user's choices
    public async Task<(bool Confirmed, int Minutes, bool NotificationsEnabled)> ShowParkingNotificationPopupAsync(
        INavigation navigation)
    {
        var tcs = new TaskCompletionSource<(bool, int, bool)>();

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
            int minutes = int.Parse(minutesPicker.SelectedItem?.ToString() ?? "10");
            bool isEnabled = enableCheckbox.IsChecked;

            await navigation.PopModalAsync();
            tcs.SetResult((true, minutes, isEnabled));
        };

        cancelButton.Clicked += async (s, e) =>
        {
            await navigation.PopModalAsync();
            tcs.SetResult((false, 0, false));
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
        await navigation.PushModalAsync(popup);

        return await tcs.Task;
    }

    // Shows parking confirmed popup with Pango option
    public async Task ShowParkingConfirmedPopupAsync(string message, INavigation navigation, Func<string, string, string, Task> displayAlert)
    {
        // Create the popup content
        var popup = new ContentPage
        {
            Title = "Parking Confirmed"
        };

        var mainLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 30,
            VerticalOptions = LayoutOptions.Center
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Parking Confirmed",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#2E7D32")
        };

        // Message
        var messageLabel = new Label
        {
            Text = message,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Colors.Black
        };

        // Buttons
        var buttonLayout = new HorizontalStackLayout
        {
            Spacing = 15,
            HorizontalOptions = LayoutOptions.Center
        };

        var okButton = new Button
        {
            Text = "OK",
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            WidthRequest = 100,
            HeightRequest = 45,
            CornerRadius = 5
        };

        var pangoButton = new Button
        {
            Text = "Pay with Pango",
            BackgroundColor = Color.FromArgb("#FF6B35"),
            TextColor = Colors.White,
            WidthRequest = 140,
            HeightRequest = 45,
            CornerRadius = 5
        };

        okButton.Clicked += async (s, e) =>
        {
            await navigation.PopModalAsync();
        };

        pangoButton.Clicked += async (s, e) =>
        {
            // TODO: Navigate to Pango app or show Pango integration
            await displayAlert("Pango", "Pango integration coming soon!", "OK");
            await navigation.PopModalAsync();
        };

        buttonLayout.Children.Add(okButton);
        buttonLayout.Children.Add(pangoButton);

        // Add all elements to main layout
        mainLayout.Children.Add(titleLabel);
        mainLayout.Children.Add(messageLabel);
        mainLayout.Children.Add(buttonLayout);

        popup.Content = new ScrollView { Content = mainLayout };

        // Show as modal
        await navigation.PushModalAsync(popup);
    }

    // Shows a popup with list of streets for user to select where they're parking
    // Returns tuple of (StreetName, SegmentId) or null if cancelled
    public async Task<(string StreetName, SegmentResponseDTO SegmentResponse)?> ShowStreetsListPopUpAsync(
        Dictionary<SegmentResponseDTO, string>? segmentToStreet,
        INavigation navigation)
    {
        if (segmentToStreet == null || segmentToStreet.Count == 0)
            return null;

        var tcs = new TaskCompletionSource<(string, SegmentResponseDTO)?>();

        // Create the popup content
        var popup = new ContentPage
        {
            Title = "Select Parking Street"
        };

        var mainLayout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 30
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Where are you parking?",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#2E7D32")
        };

        var subtitleLabel = new Label
        {
            Text = "Select the street you're parking on:",
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Get unique streets with their segment IDs
        var streetGroups = segmentToStreet
            .GroupBy(kvp => kvp.Value)
            .Select(g => new { StreetName = g.Key, SegmentResponse = g.First().Key })
            .OrderBy(s => s.StreetName)
            .ToList();

        // Create scrollable list of streets
        var scrollView = new ScrollView
        {
            HeightRequest = 650
        };

        var streetsList = new VerticalStackLayout
        {
            Spacing = 10
        };

        foreach (var street in streetGroups)
        {
            var streetButton = new Button
            {
                Text = street.StreetName,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2E7D32"),
                BorderColor = Color.FromArgb("#2E7D32"),
                BorderWidth = 2,
                CornerRadius = 5,
                Padding = new Thickness(15),
                HorizontalOptions = LayoutOptions.Fill
            };

            streetButton.Clicked += async (s, e) =>
            {
                await navigation.PopModalAsync();
                tcs.SetResult((street.StreetName, street.SegmentResponse));
            };

            streetsList.Children.Add(streetButton);
        }

        scrollView.Content = streetsList;

        // Cancel button
        var cancelButton = new Button
        {
            Text = "Cancel",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#666666"),
            WidthRequest = 100,
            HeightRequest = 45,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        cancelButton.Clicked += async (s, e) =>
        {
            await navigation.PopModalAsync();
            tcs.SetResult(null);
        };

        // Add all elements to main layout
        mainLayout.Children.Add(titleLabel);
        mainLayout.Children.Add(subtitleLabel);
        mainLayout.Children.Add(scrollView);
        mainLayout.Children.Add(cancelButton);

        popup.Content = mainLayout;

        // Show as modal
        await navigation.PushModalAsync(popup);

        return await tcs.Task;
    }
}

    
