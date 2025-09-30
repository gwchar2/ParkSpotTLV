using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Core.Models;
using ParkSpotTLV.Infrastructure;
using System.Security.Claims;

namespace ParkSpotTLV.Api.Endpoints {

    public static class ParkingEndpoints {
        public static IEndpointRouteBuilder MapParking(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/parking").WithTags("Parking Related Requests").RequireAuthorization();


            /* Get /id  Free Parking Budget
             * Accepts: Permit ID
             * Returns: 
             *      200 List<PermitTimeLeft> - Returns the amount of free parking time left for all permits
             *      401 Unauthorized access - Access token is expired or no such user.
             */



            /* Post /start  Start Parking
             * Accepts: Permit ID
             * Returns: 
             *      200 (IQueryable<TimeSpan>) - Returns the amount of free parking time left for all permits
             *      401 Unauthorized access - Access token is expired or no such user.
             *      404 Not Found - Permit not found for ID id 
             */



            /* Post /stop  Stop Parking
            * Accepts: Permit ID
            * Returns: 
            *      200 (IQueryable<TimeSpan>) - Returns the amount of free parking time left for all permits
            *      401 Unauthorized access - Access token is expired or no such user.
            *      404 Not Found - Permit not found for ID id 
            */

            return group;
        }
    }
}
