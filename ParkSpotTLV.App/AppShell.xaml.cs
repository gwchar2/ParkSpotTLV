using ParkSpotTLV.App.Controls;
using ParkSpotTLV.App.Pages;

namespace ParkSpotTLV.App {
    public partial class AppShell : Shell {
        private MenuOverlay? _currentMenuOverlay;
        private readonly IServiceProvider _services;


        public AppShell(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
            Routing.RegisterRoute("ShowMapPage", typeof(ShowMapPage));
            Routing.RegisterRoute("AddCarPage", typeof(AddCarPage));
            Routing.RegisterRoute("EditCarPage", typeof(EditCarPage));
            Routing.RegisterRoute("AccountDetailsPage", typeof(AccountDetailsPage));

            // Subscribe to navigation events to show/hide menu button
            Navigated += OnShellNavigated;
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            UpdateMenuButtonVisibility();
        }

        private void UpdateMenuButtonVisibility()
        {
            if (Current?.CurrentPage is ContentPage contentPage)
            {
                // Hide menu button on login/signup pages
                bool shouldHideMenu = contentPage.GetType() == typeof(MainPage) ||
                                     contentPage.GetType() == typeof(SignUpPage);

                MenuButton.IsVisible = !shouldHideMenu;
            }
        }

        private async void OnMenuButtonClicked(object sender, EventArgs e)
        {
            if (Current?.CurrentPage is ContentPage contentPage)
            {
                // Remove existing menu overlay if any
                RemoveMenuOverlay();

                // Create new menu overlay
                _currentMenuOverlay =  _services.GetRequiredService<MenuOverlay>();

                // Create an absolute layout overlay
                var overlayContainer = new AbsoluteLayout();
                AbsoluteLayout.SetLayoutBounds(_currentMenuOverlay, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(_currentMenuOverlay, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
                overlayContainer.Children.Add(_currentMenuOverlay);

                // Store original content
                var originalContent = contentPage.Content;

                // Create a new grid with the original content and overlay
                var rootGrid = new Grid();
                rootGrid.Children.Add(originalContent);
                rootGrid.Children.Add(overlayContainer);

                // Set as page content
                contentPage.Content = rootGrid;

                await _currentMenuOverlay.ShowMenu();
            }
        }

        public void RemoveMenuOverlay()
        {
            if (_currentMenuOverlay != null && Current?.CurrentPage is ContentPage contentPage)
            {
                // Restore original content
                if (contentPage.Content is Grid rootGrid && rootGrid.Children.Count >= 2)
                {
                    var originalContent = rootGrid.Children[0];
                    rootGrid.Children.Remove(originalContent);
                    if (originalContent is View originalView)
                    {
                        contentPage.Content = originalView;
                    }
                }
            }
            _currentMenuOverlay = null;
        }
    }
}
