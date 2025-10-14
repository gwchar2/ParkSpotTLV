namespace ParkSpotTLV.Api.Endpoints.Support {
    public static class UserContext {

        public static readonly object UserIdKey = new();
        public static readonly object VehicleId = new();
        public static readonly object ExpectedXmin = new();

        public static Guid GetUserId(this HttpContext ctx)
            => ctx.Items.TryGetValue(UserIdKey, out var userId) && userId is Guid key
                ? key
                : throw new InvalidOperationException();
        public static Guid GetVehicleId(this HttpContext ctx)
            => ctx.Items.TryGetValue(VehicleId, out var vehicleId) && vehicleId is Guid key
                ? key
                : throw new InvalidOperationException();
        public static uint GetXmin(this HttpContext ctx)
            => ctx.Items.TryGetValue(ExpectedXmin, out var expectedXmin) && expectedXmin is uint key
                ? key
                : throw new InvalidOperationException();
    }

}
