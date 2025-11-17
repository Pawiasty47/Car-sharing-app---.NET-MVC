namespace projekt_zespołowy.Models
{
    public class PassengerProfile
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        public double Rating { get; set; }
        public int CompletedBookingsCount { get; set; }
        public bool PrefersNonSmoking { get; set; } = true;
        public bool PrefersQuietRide { get; set; } = false;
    }
}
