
using System.ComponentModel.DataAnnotations;


namespace ParkSpotTLV.Infrastructure.Auth.Models {

    public readonly record struct RefreshRotateResult(
        string AccessToken,
        [Required] DateTimeOffset AccessExpiresAtUtc,
        string RefreshToken,
        [Required] DateTimeOffset RefreshExpiresAtUtc
        );
}
