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

        public Guid RideId { get; set; }
        [ForeignKey("RideId")]
        public virtual OfferedRide Ride { get; set; }

        public Guid PassengerUserId { get; set; }
        [ForeignKey("PassengerUserId")]
        public virtual User Passenger { get; set; }

        public int SeatsRequested { get; set; } = 1;

        // NOWE POLE: Kwota zablokowana na portfelu pasażera w momencie wysłania prośby
        [Column(TypeName = "decimal(18,2)")]
        public decimal FrozenAmount { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotRequired;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CommentByPassenger { get; set; }
        public string? CommentByDriver { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}