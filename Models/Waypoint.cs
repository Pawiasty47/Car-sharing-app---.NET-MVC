using projekt_zespołowy.Models;

public class Waypoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RideId { get; set; }
    public OfferedRide Ride { get; set; }

    public Guid LocationPointId { get; set; }
    public LocationPoint Location { get; set; }

    public int Order { get; set; }
}