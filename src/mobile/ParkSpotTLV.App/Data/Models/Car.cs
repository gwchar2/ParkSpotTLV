namespace ParkSpotTLV.App.Data.Models;
/*
* Local object to represent a Viehcle 
*/
public class Car
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CarType Type { get; set; } = CarType.Private;
    public bool HasResidentPermit { get; set; } = false;
    public int ResidentPermitNumber { get; set; } = 0;
    public bool HasDisabledPermit { get; set; } = false;

    public string TypeDisplayName => Type switch
    {
        CarType.Private => "Private",
        CarType.Truck => "Truck",
        _ => "Private"
    };
}

public enum CarType
{
    Private,
    Truck
}