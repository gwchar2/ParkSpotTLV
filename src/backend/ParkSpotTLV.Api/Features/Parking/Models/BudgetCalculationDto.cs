namespace ParkSpotTLV.Api.Features.Parking.Models {


    /*
     * DTO from evaluation to budget calculation service
     */
    public sealed record BudgetCalculationDTO(
        int TotalMinutes,
        int PaidMinutes,
        int FreeMinutes,
        int FreeMinutesCharged,
        int RemainingToday
    );
}
