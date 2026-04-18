namespace projekt_zespołowy.Models
{
    public enum RideStatus { Draft, Published, InProgress, Completed, Cancelled}
    public class OfferedRide
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DriverId { get; set; }
        public DriverProfile Driver { get; set; }
        public Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
        public Guid StartLocationId { get; set; }
        public LocationPoint StartLocation { get; set; }
        public Guid EndLocationId { get; set; }
        public LocationPoint EndLocation { get; set; }
        public ICollection<Waypoint> Waypoints { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int SeatsOffered { get; set; }
        public int SeatsTaken { get; set; }
        public decimal PricePerSeat { get; set; }
        public bool IsFlexiblePrice { get; set; } = false;
        public RideStatus Status { get; set; } = RideStatus.Draft;
        public string? Notes { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
