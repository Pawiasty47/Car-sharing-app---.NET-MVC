using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projekt_zespołowy.Models
{
    public enum BookingStatus { Pending, Confirmed, Rejected, Cancelled, Completed }
    public enum PaymentStatus { NotRequired, Pending, Paid, Refunded }

    public class Booking
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relacja do Przejazdu
        public Guid RideId { get; set; }
        [ForeignKey("RideId")]
        public virtual OfferedRide Ride { get; set; }

        // Relacja do Pasażera (Użytkownika systemu)
        public Guid PassengerUserId { get; set; }
        [ForeignKey("PassengerUserId")]
        public virtual User Passenger { get; set; } // Zmieniono z PassengerProfile na User

        public int SeatsRequested { get; set; } = 1;

        public BookingStatus Status { get; set; } = BookingStatus.Confirmed; // Domyślnie potwierdzona dla uproszczenia MVP
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotRequired;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CommentByPassenger { get; set; } // Zmieniono na nullable (?)
        public string? CommentByDriver { get; set; }    // Zmieniono na nullable (?)
        public virtual Payment? Payment { get; set; }
    }
}