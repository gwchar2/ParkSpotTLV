namespace ParkSpotTLV.App {
    public partial class AppShell : Shell {
        public AppShell() {
            InitializeComponent();

            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
        }
    }
}
