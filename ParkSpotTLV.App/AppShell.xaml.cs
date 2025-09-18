namespace ParkSpotTLV.App {
    public partial class AppShell : Shell {
        public AppShell() {
            InitializeComponent();

            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
            Routing.RegisterRoute("ShowMapPage", typeof(ShowMapPage));
            Routing.RegisterRoute("AddCarPage", typeof(AddCarPage));
            Routing.RegisterRoute("MyCarsPage", typeof(MyCarsPage));
        }
    }
}
