using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models.ViewModels
{
    public class PassengerProfileViewModel
    {
        // Dane osobowe
        [Display(Name = "Imię")]
        [Required(ErrorMessage = "Imię jest wymagane")]
        public string FirstName { get; set; }

        [Display(Name = "Nazwisko")]
        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Telefon")]
        [Phone]
        public string? PhoneNumber { get; set; }

        // Zdjęcie
        [Display(Name = "Zdjęcie profilowe")]
        public IFormFile? ProfilePictureFile { get; set; } // To służy do przesłania pliku
        public byte[]? ExistingProfilePicture { get; set; } // To służy do wyświetlenia obecnego

        // Preferencje
        public bool PrefersNonSmoking { get; set; }
        public bool PrefersQuietRide { get; set; }
        public bool PrefersMusic { get; set; }
        public bool AcceptsPets { get; set; }
        public bool AcceptsEating { get; set; }
    }
}