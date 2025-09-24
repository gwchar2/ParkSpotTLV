

namespace ParkSpotTLV.Contracts.Auth {
    public sealed record LogoutRequest (
        string RefreshToken,
        bool AllDevices = false
        );
}
