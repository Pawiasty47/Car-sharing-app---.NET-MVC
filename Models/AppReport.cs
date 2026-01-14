using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models
{
    public enum ReportCategory
    {
        [Display(Name = "Błąd techniczny")]
        Technical,
        [Display(Name = "Problem z płatnością")]
        Billing,
        [Display(Name = "Niewłaściwe zachowanie użytkownika")]
        UserBehavior,
        [Display(Name = "Inne")]
        Other
    }

    public class AppReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relacja do User (IdentityUser<Guid>)
        public Guid? UserId { get; set; }
        public virtual User? User { get; set; }

        public ReportCategory Category { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;
    }
}
