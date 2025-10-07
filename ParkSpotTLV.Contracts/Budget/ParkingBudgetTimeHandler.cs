

namespace ParkSpotTLV.Contracts.Budget {
    public static class ParkingBudgetTimeHandler {

        public static readonly TimeOnly ResetTime = new(8, 0); // A read-only default for 8am reset hour

        /*
         * Given a local time, we return the anchor date of its 8-8 window
         */
        public static DateOnly AnchorDateFor(DateTimeOffset localTime) {

            var today = new TimeOnly(localTime.Hour, localTime.Minute, localTime.Second);
            var date = DateOnly.FromDateTime(localTime.Date);

            return today >= ResetTime ? date : date.AddDays(-1);

        }

        public static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> SliceByAnchorBoundary(DateTimeOffset startLocal, DateTimeOffset endLocal) {

            var reset = new TimeSpan(8, 0, 0);
            var current = startLocal;

            while (true) {
                var baseDate = current.TimeOfDay >= reset
                    ? DateOnly.FromDateTime(current.Date)
                    : DateOnly.FromDateTime(current.Date).AddDays(-1);

                var boundaryLocal = new DateTimeOffset(
                    baseDate.AddDays(1).ToDateTime(ParkingBudgetTimeHandler.ResetTime),
                    current.Offset);

                var sliceEnd = boundaryLocal < endLocal ? boundaryLocal : endLocal;
                yield return (current, sliceEnd);

                if (sliceEnd >= endLocal) break;
                current = sliceEnd;
            }
        }

    }
}
