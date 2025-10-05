using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Api.Services.Evaluation.Logic;
using ParkSpotTLV.Api.Services.Evaluation.Query;
using ParkSpotTLV.Api.Services.Evaluation.Specs;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Facade {
    /*
     * Manages everything, query -> availability -> price -> classification -> filtering.
     */
    public sealed class MapSegmentsEvaluator(
        ISegmentQueryService query, ITariffCalendarService calendar, IPaymentDecisionService pricing,
        IAvailabilityService availability, IClassificationService classification) : IMapSegmentsEvaluator{

        private readonly ISegmentQueryService _query = query;
        private readonly ITariffCalendarService _calendar = calendar;
        private readonly IPaymentDecisionService _pricing = pricing;
        private readonly IAvailabilityService _availability = availability;
        private readonly IClassificationService _classification = classification;


        public async Task<IReadOnlyList<SegmentResult>> EvaluateAsync(MapSegmentsRequest request, CancellationToken ct) {

            var rows = await _query.GetViewportAsync(request.MinLon, request.MaxLon, request.MinLat, request.MaxLat, request.CenterLon ,request.CenterLat, ct);
            var now = request.Now;

            var results = new List<SegmentResult>(rows.Count);

            foreach (var row in rows) {

                // Can user park in this segment at this time? Availability(DateTimeOffset? AvailableFrom ,DateTimeOffset? AvailableUntil, DateTimeOffset? NextChange);
                var availabilityNow = _availability.Compute(row.ParkingType, row.ZoneCode, row.Tariff, now, request.Pov);

                // Is this segment currently in active hours? TariffCalendarStatus(bool ActiveNow,DateTimeOffset? NextStart,DateTimeOffset? NextEnd);
                var calNow = _calendar.GetStatus(row.Tariff, now);

                // Payment decision on start PriceDecision(PriceNow { Free, Paid } Price,string Reason,int? FreeBudgetRemainingMinutes = null);
                var paymentDecisionNow = await _pricing.DecideAsync(row.ParkingType, row.ZoneCode, row.Tariff, now, request.Pov, calNow.ActiveNow, ct);
                
                // We get the PaymentDecision when the tariff starts.
                DateTimeOffset? payOnStart = calNow.ActiveNow ? now : (calNow.NextStart is DateTimeOffset nextStart && nextStart > now ? nextStart : null);
                PaymentDecision? decisionAtPaidStart = null;
                if (payOnStart is DateTimeOffset payStart) {
                    var calAtStart = _calendar.GetStatus(row.Tariff, payStart);   
                    decisionAtPaidStart = await _pricing.DecideAsync( row.ParkingType, row.ZoneCode, row.Tariff, payStart, request.Pov, calAtStart.ActiveNow, ct);
                }

                // Classification (string Group (Free / Paid / Limited / Restricted) , string Reason, bool IsLegalNow)
                var (group, reason, payNow, payLater) = _classification.Classify(row.ParkingType, availabilityNow, 
                    paymentDecisionNow, decisionAtPaidStart, calNow, now, request.MinParkingTime);

                // We will now adjust AvailableUntil variable according to the start time of next payment segment (if exists) including the daily budget it has.
                var adjustedNextChange = availabilityNow.NextChange;
                    // If the permit is active, the next change will be once the free budget remaining is done.
                if (calNow.ActiveNow 
                    && paymentDecisionNow.Reason == "RemainingDailyBudget" 
                    && paymentDecisionNow.FreeBudgetRemainingMinutes is int remainingNow 
                    && remainingNow > 0) 
                    adjustedNextChange = now.AddMinutes(remainingNow);

                    // If the permit is free at the moment, we look at the 'future' start of payment, and add the remaining daily budget to it.
                else if (payOnStart is DateTimeOffset futurePaidStart 
                    && decisionAtPaidStart is not null 
                    && decisionAtPaidStart.PayNow == PaymentNow.Free 
                    && decisionAtPaidStart.Reason == "RemainingDailyBudget" 
                    && decisionAtPaidStart.FreeBudgetRemainingMinutes is int remainingAtStart 
                    && remainingAtStart > 0)
                    adjustedNextChange = futurePaidStart.AddMinutes(remainingAtStart);

                    // If the segment is privileged, we will adjust the time accordingly (Its limited and we dont need free budget taken into consideration)
                if (row.ParkingType == ParkingType.Privileged
                    && availabilityNow.NextChange is DateTimeOffset privStart
                    && privStart > now
                    && adjustedNextChange is DateTimeOffset userChange
                    && privStart < userChange)
                    adjustedNextChange = privStart;

                // Build result
                results.Add(new SegmentResult(
                    row.SegmentId, row.ZoneCode, row.Tariff,
                    row.NameEnglish,
                    row.NameHebrew,
                    Group: group,
                    Reason: reason,
                    row.ParkingType,
                    IsPayNow: payNow,
                    IsPaylater: payLater,
                    AvailableFrom: availabilityNow.AvailableFrom,
                    AvailableUntil: availabilityNow.AvailableUntil ?? availabilityNow.AvailableFrom?.AddMinutes(request.MinParkingTime),
                    NextChange: adjustedNextChange,
                    FreeBudgetRemaining: paymentDecisionNow.FreeBudgetRemainingMinutes,
                    Geom: row.Geom
                ));
            }

            return results;
        }
    }
}
