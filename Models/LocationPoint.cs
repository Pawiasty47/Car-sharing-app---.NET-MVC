namespace projekt_zespołowy.Models
{
    public class LocationPoint
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longtitude { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public ICollection<Waypoint> Waypoints { get; set; }
    }
}
