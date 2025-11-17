namespace projekt_zespołowy.Models
{
    public class CityIncentive
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Description { get; set; }
        public bool ForDrivers { get; set; } = true;
        public bool ForPassengers { get; set; } = false;
        public int? MinRidesPerMonth { get; set; }
        public bool RequiresVerification { get; set; } = false;
    }
}
