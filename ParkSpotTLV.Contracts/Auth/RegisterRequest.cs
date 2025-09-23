
namespace ParkSpotTLV.Contracts.Auth {

    /* incoming payload (Username, Password, optional DeviceName). */
    public sealed record RegisterRequest (

        string Username,
        string Password

    );
}
