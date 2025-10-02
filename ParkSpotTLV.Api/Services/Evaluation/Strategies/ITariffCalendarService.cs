

using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    /*
     * Together with Tariff + now, we return the currently active, and the next in line time boundaries.
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
