

using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    /*
     * Checks if the time given falls in active hours
     */
    public interface ITariffCalendarService {

        TariffCalendarStatus GetStatus(Tariff tariff, DateTimeOffset now);

    }

    public sealed record TariffCalendarStatus(

        bool ActiveNow,
        DateTimeOffset? NextStart,
        DateTimeOffset? NextEnd
        
        );

}
