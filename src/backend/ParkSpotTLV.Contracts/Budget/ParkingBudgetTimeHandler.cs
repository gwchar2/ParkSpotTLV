

namespace ParkSpotTLV.Contracts.Budget {
    public static class ParkingBudgetTimeHandler {

        public static readonly TimeOnly ResetTime = new(8, 0); // A read-only default for 8am reset hour

        /*
         * Given a local time, we return the anchor date of its 8-8 window
         */
        public static DateOnly AnchorDateFor(DateTimeOffset currentTime) {

            var today = new TimeOnly(currentTime.Hour, currentTime.Minute, currentTime.Second);
            var date = DateOnly.FromDateTime(currentTime.Date);

            return today >= ResetTime ? date : date.AddDays(-1);
        }
    }
}
