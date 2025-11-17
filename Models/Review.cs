namespace projekt_zespołowy.Models
{
    public class Review
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FromUserId { get; set; }
        public User FromUser { get; set; }
        public Guid ToUserId { get; set; }
        public User ToUser { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public Guid? RideId { get; set; }
        public OfferedRide Ride { get; set; }
    }
}
