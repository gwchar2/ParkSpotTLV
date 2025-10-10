

namespace ParkSpotTLV.Contracts.Auth {
    public sealed record UpdatePasswordRequest (
        string OldPassword,
        string NewPassword
    );
}
