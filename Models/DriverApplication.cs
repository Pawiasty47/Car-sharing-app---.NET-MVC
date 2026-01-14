using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projekt_zespołowy.Models
{
    public enum ApplicationStatus
    {
        Pending,    // Oczekujący
        Approved,   // Zaakceptowany
        Rejected    // Odrzucony
    }

    public class DriverApplication
    {
        [Key]
        public int Id { get; set; }

        // Kto składa wniosek
        public Guid UserId { get; set; }
        public User User { get; set; }

        // --- TU DODALIŚMY NOWE POLA ---
        [Required]
        public string FirstName { get; set; } // Imię

        public string? SecondName { get; set; } // Drugie imię (opcjonalne)

        [Required]
        public string LastName { get; set; } // Nazwisko

        [Required]
        public string IdDocumentNumber { get; set; } // Numer dowodu/paszportu
        // ------------------------------

        // Dane do weryfikacji
        [Required]
        public string DriverLicenseNumber { get; set; } // Numer prawa jazdy

        [Required]
        public string LicenseCategories { get; set; } // np. "B, B1"

        [Required]
        public DateTime DateOfBirth { get; set; } // Do sprawdzenia wieku (18 lat)

        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string PESEL { get; set; } // Do walidacji cyfr

        // Pojazd podpięty do wniosku
        public Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        // Status i odpowiedź admina
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public string? AdminFeedback { get; set; } // Powód odrzucenia
        public DateTime ApplicationDate { get; set; } = DateTime.Now;
    }
}