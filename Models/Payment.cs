using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projekt_zespołowy.Models
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BookingId { get; set; }

        [ForeignKey("BookingId")] 
        public virtual Booking Booking { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN"; 

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}