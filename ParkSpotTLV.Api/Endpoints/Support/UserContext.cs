namespace ParkSpotTLV.Api.Endpoints.Support {
    public static class UserContext {

        public static readonly object UserIdKey = new();
        public static readonly object OwnerIdKey = new();

        public static Guid GetUserId(this HttpContext ctx)
            => ctx.Items.TryGetValue(UserIdKey, out var userId) && userId is Guid key
                ? key
                : throw new InvalidOperationException();


    }

}
