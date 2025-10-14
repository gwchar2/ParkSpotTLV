namespace ParkSpotTLV.Api.Features.Parking.Models {
    public sealed record BudgetCalculationDTO(
        int TotalMinutes,
        int PaidMinutes,
        int FreeMinutes,
        int FreeMinutesCharged,
        int RemainingToday
    );
}
