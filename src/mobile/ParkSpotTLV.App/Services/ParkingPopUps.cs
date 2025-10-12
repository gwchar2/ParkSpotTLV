using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Services;

// Handles parking-related UI operations including popups and permits
public class ParkingPopUps
{
    private readonly CarService _carService;

    public ParkingPopUps(CarService carService)
    {
        _carService = carService ?? throw new ArgumentNullException(nameof(carService));
    }

    // When user has disabled permit - he is allowed to choose to not use in this parking session
    // Shows permit selection dialog and returns the selected permit ID
    public async Task<(Guid? activePermitId, bool isResidential)> ShowPermitPopupAsync(string? pickedCarId, Func<string, string?, string?, string[], Task<string>> displayActionSheet)
    {
        // check if pop up required
        if (pickedCarId is null)
            return (null,false);
        // get Car
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
                    return (await _carService.GetPermitAsync(pickedCarId, 2),false); // get default permit
                }
            }
        }
    }

    

    // Shows parking confirmed alert
    public async Task ShowParkingConfirmedPopupAsync(int budgetRemaining,INavigation navigation, Func<string, string, string, Task> displayAlert)
    {
        // Build message
        string message = "Parking started!";
        
        message += "\nParking outside your zone.";
        message += $"\nYou have {budgetRemaining} minutes of free parking.";

        // Show simple alert
        await displayAlert("Parking Confirmed", message, "OK");
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
            TextColor = Color.FromArgb("#FF2B3271") // Primary
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
            .GroupBy(segmentStreetPair => segmentStreetPair.Value)
            .Select(streetGroup => new { StreetName = streetGroup.Key, SegmentResponse = streetGroup.First().Key })
            .OrderBy(street => street.StreetName)
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
                TextColor = Color.FromArgb("#FF2B3271"), // Primary
                BorderColor = Color.FromArgb("#FF2B3271"), // Primary
                BorderWidth = 2,
                CornerRadius = 8,
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
            TextColor = Color.FromArgb("#FF757575"), // Secondary
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

    
