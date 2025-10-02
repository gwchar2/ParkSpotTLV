using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Api.Services.Evaluation.Logic;
using ParkSpotTLV.Api.Services.Evaluation.Query;
using ParkSpotTLV.Api.Services.Evaluation.Specs;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;

namespace ParkSpotTLV.Api.Services.Evaluation.Facade {
    /*
     * Manages everything, query -> availability -> price -> classification -> filtering.
     */
    public sealed class MapSegmentsEvaluator(
        ISegmentQueryService query, ITariffCalendarService calendar, IPrivilegedPolicyService privileged, IPriceDecisionService pricing,
        IAvailabilityService availability, IClassificationService classification, IMinDurationSpec minDuration) : IMapSegmentsEvaluator{

        private readonly ISegmentQueryService _query = query;
        private readonly ITariffCalendarService _calendar = calendar;
        private readonly IPrivilegedPolicyService _privileged = privileged;
        private readonly IPriceDecisionService _pricing = pricing;
        private readonly IAvailabilityService _availability = availability;
        private readonly IClassificationService _classification = classification;
        private readonly IMinDurationSpec _minDuration = minDuration;


        public async Task<IReadOnlyList<SegmentResult>> EvaluateAsync(MapSegmentsRequest request, CancellationToken ct) {

            var rows = await _query.GetViewportAsync(request.MinLon, request.MinLat, request.MaxLon, request.Maxlat, ct);
            var now = request.Now;

            var results = new List<SegmentResult>(rows.Count);

            foreach (var row in rows) {

                // Get availability
                var avail = _availability.Compute(row.ParkingType, row.ZoneCode, row.Tariff, now, request.Pov);

                // If minimum duration is not satisfied, we will continue
                if (!_minDuration.IsSatisfied(avail, now, request.MinDurationMinutes))
                    continue;

                // Check if paid is active
                var cal = _calendar.GetStatus(row.Tariff, now);
                var price = await _pricing.DecideAsync(row.ParkingType, row.ZoneCode, row.Tariff, now, request.Pov, cal.ActiveNow, ct);

                // Classify properly Free/Paid/Limited/Illegal
                var (group, reason, isLegalNow) = _classification.Classify(row.ParkingType, avail, price, now, request.LimitedThresholdMinutes);

                // Construct the result
                results.Add(new SegmentResult(
                    row.SegmentId,
                    row.ZoneCode,
                    row.Tariff,
                    row.ParkingType,
                    Group: group,
                    Reason: reason,
                    IsLegalNow: isLegalNow,
                    AvailableFrom: avail.AvailableFrom,
                    AvailableUntil: avail.AvailableUntil,
                    NextChange: avail.NextChange,
                    PriceNow: price.Price == PriceNow.Paid,
                    FreeBudgetRemaining: price.FreeBudgetRemainingMinutes
                ));
            }


            // We always return all groups; FE decides visibility based on flags.
            return results;
        }
    }
}
