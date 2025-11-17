namespace projekt_zespołowy.Models
{
    public class Vehicle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }
        public DriverProfile Owner { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string RegistrationNumber { get; set; }
        public int SeatsTotal { get; set; }
        public int SeatsAvailable { get; set; }
        public string Color { get; set; }
    }
}
