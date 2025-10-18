
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;
using System.Reflection;
using System.Security.Claims;
using ParkSpotTLV.Api.Endpoints.Support.Errors;

namespace ParkSpotTLV.Api.Endpoints.Support {
    public static class Guards {

        /*
         * Trys to retrieve user ID from request
         */
        public static bool TryGetUserId(HttpContext ctx,out Guid userId, out IResult? problem) {
            problem = null;

            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out userId)) {
                problem = GeneralErrors.ExpiredToken(ctx);
                return false;
            }
            return true;
        }
        /*
         * Ensures vehicle ownership
         */
        public static async Task<IResult?> EnsureVehicleOwnershipAsync(HttpContext ctx, AppDbContext db, Guid vehicleId, CancellationToken ct) {
            if (vehicleId == Guid.Empty)    return VehicleErrors.ForbiddenId(ctx);

            var userId = ctx.GetUserId();

            var isOwner = await db.Vehicles.AsNoTracking().AnyAsync(v => v.Id == vehicleId && v.OwnerId == userId, ct);

            return isOwner ? null : VehicleErrors.Forbidden(ctx);
        }

        /*
         * Parse base64 row version (xmin) from JSON body property "rowVersion".
         * All 3 functions are used for xmin. Last one is the call for the first 2.
         */
        public static bool TryGetRowVersionFromArgs(EndpointFilterInvocationContext context, out string? rowVersion) {
            foreach (var arg in context.Arguments) {
                if (arg is null) continue;

                var prop = arg.GetType().GetProperty("RowVersion",BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop is null) continue;

                if (prop.GetValue(arg) is string s && !string.IsNullOrWhiteSpace(s)) {
                    rowVersion = s;
                    return true;
                }
            }

            rowVersion = null;
            return false;
        }
        /*
         * Trys to parse the row version from base 64
         */
        public static bool TryParseRowVersionBase64(string? base64, out uint xmin) {
            xmin = 0;
            if (string.IsNullOrWhiteSpace(base64)) return false;

            try {
                var bytes = Convert.FromBase64String(base64);
                if (bytes.Length != 4) return false;
                xmin = BitConverter.ToUInt32(bytes, 0); // little-endian
                return true;
            }
            catch {
                return false;
            }
        }
        /*
         * Just calls the 2 functions above
         */
        public static bool TryGetExpectedXmin(EndpointFilterInvocationContext context, out uint xmin) {
            xmin = 0;
            return TryGetRowVersionFromArgs(context, out var rv)
                   && TryParseRowVersionBase64(rv, out xmin);
        }


    }
}