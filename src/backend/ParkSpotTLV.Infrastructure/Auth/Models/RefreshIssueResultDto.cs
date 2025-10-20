
using System.ComponentModel.DataAnnotations;


namespace ParkSpotTLV.Infrastructure.Auth.Models {
    public readonly record struct RefreshIssueResult(
        string RefreshToken,
        [Required] DateTimeOffset ExpiresAtUtc
        );
}
