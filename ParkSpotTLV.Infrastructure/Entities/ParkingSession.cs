using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Infrastructure.Entities {
    public class ParkingSession {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VehicleId { get; set; }
        public Guid SegmentId { get; set; }


        // basically everything from segment DTO goes here 
        public string Group { get; set; } = default!;                              //"FREE" -> Free the entire duration /  "PAID" -> Paid some time during the duration / "LIMITED" -> Turns to restricted / "RESTRICTED" -> Always restricted                                      
        public string? Reason { get; set; }
        public ParkingType ParkingType { get; set; }                         // "Free" / "Paid" / "Privileged"
        public int? ZoneCode { get; set; }
        public Tariff Tariff { get; set; }

        public bool IsPayNow { get; set; }
        public bool IsPayLater { get; set; }
        public DateTimeOffset? NextChangeUtc { get; set; }               


        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset? StoppedUtc { get; set; }
        public DateTimeOffset PlannedEndUtc { get; set; }


        public int ParkingBudgetUsed { get; set; }
        public int PaidMinutes { get; set; }
        public ParkingSessionStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }

    }
}
