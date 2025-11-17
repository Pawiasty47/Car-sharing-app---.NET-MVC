using System.Text.Json;

namespace projekt_zespołowy.Models
{
    public enum BookingStatus { Pending, Confirmed, Rejected, Cancelled, Completed}
    public enum PaymentStatus { NotRequired, Pending, Paid, Refunded}
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RideId { get; set; }
        public OfferedRide Ride { get; set; }
        public Guid PassengerUserId { get; set; }
        public PassengerProfile Passenger { get; set; }
        public int SeatsRequested { get; set; } = 1;
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotRequired;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CommentByPassenger { get; set; }
        public string CommentByDriver { get; set; }
        public Payment Payment { get; set; }
    }
}
