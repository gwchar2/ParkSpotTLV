using ParkSpotTLV.Contracts.StreetSegments;
using ParkSpotTLV.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts;
using System.Security.Claims;


namespace ParkSpotTLV.Api.Endpoints {
    public static class StreetEndpoints {

        /* Possible preferences:
         * Paid parking ->  
         * free parking ->
         * restricted ->  
         * specific hours ->  
         * minimum parking time -> 
         * has zone permit -> 
         * has disabled permit ->
         */

        public static IEndpointRouteBuilder MapStreets(this IEndpointRouteBuilder routes) {

            var streets = routes.MapGroup("/streets").WithTags("Streets").RequireAuthorization();

           



            return streets;

        }

    }
}
