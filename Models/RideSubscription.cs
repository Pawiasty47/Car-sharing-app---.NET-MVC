using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projekt_zespołowy.Models
{
    public class RideSubscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required(ErrorMessage = "Podaj miasto początkowe.")]
        [Display(Name = "Skąd")]
        public string FromCity { get; set; }

        [Required(ErrorMessage = "Podaj miasto docelowe.")]
        [Display(Name = "Dokąd")]
        public string ToCity { get; set; }

        [Required(ErrorMessage = "Podaj datę przejazdu.")]
        [Display(Name = "Kiedy")]
        [DataType(DataType.Date)]
        public DateTime RideDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}