namespace projekt_zespołowy.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public virtual DriverProfile DriverProfile { get; set; }
        public virtual PassengerProfile PassengerProfile { get; set; }
        public virtual ICollection<Review> ReviewsGiven { get; set; }
        public virtual ICollection<Review> ReviewsReceived { get; set; }
    }
}
