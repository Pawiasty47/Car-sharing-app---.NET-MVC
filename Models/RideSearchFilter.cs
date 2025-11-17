namespace projekt_zespołowy.Models
{
    public class RideSearchFilter
    {
        public string FromCity { get; set; }
        public string ToCity { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int MinSeats { get; set; } = 1;
        public bool OnlyVerifiedDrivers { get; set; } = false;
        public decimal? MaxPricePerSeat { get; set; }
    }
}
