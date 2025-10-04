
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Map {
    // Same thing as segment result in API but with string enums 
    public sealed record SegmentResponseDTO (

        Guid SegmentId,
        string Tariff,
        int? ZoneCode,
        string? NameEnglish,
        string? NameHebrew,
        string Group, //"FREE" -> Free the entire duration /  "PAID" -> Paid some time during the duration / "LIMITED" -> Turns to restricted / "RESTRICTED" -> Always restricted                                      
        string Reason,
        string ParkingType,                                 // "Free" / "Paid" / "Privileged"
        bool IsPayNow,                                      // True if parking costs money at this moment
        bool IsPaylater,                                    // True if segment will costs money at any time during parking
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange,
        int? FreeBudgetRemaining,
        System.Text.Json.JsonElement Geometry             // GeoJson object
    );
}
