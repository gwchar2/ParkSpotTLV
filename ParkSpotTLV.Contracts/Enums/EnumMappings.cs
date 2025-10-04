

namespace ParkSpotTLV.Contracts.Enums {
    public class EnumMappings {

        public static string MapTariff(Tariff t)
                        => t == Tariff.City_Outskirts
                           ? "City Outskirts"
                           : "City Center";

        public static string MapParkingType(ParkingType p) => p 
            switch {
                ParkingType.Paid => "Paid",
                ParkingType.Privileged => "Privileged",
                _ => "Free"
        };

        public static string MapPermitType(PermitType p) => p
            switch {
                PermitType.Default => "Default",
                PermitType.Disability => "Disability",
                _ => "ZoneResident"
            };

        public static string MapVehicleType(VehicleType v) => v
            switch {
                VehicleType.Car => "Car",
                _ => "Truck"
            };

    }
}
