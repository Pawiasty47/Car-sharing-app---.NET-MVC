using Microsoft.AspNetCore.Http;

namespace projekt_zespołowy.Models.ViewModels
{
    public class PassengerProfileViewModel
    {
        // Dane użytkownika
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }

        // Preferencje
        public bool PrefersNonSmoking { get; set; }
        public bool PrefersQuietRide { get; set; }
        public bool PrefersMusic { get; set; }
        public bool AcceptsPets { get; set; }
        public bool AcceptsEating { get; set; }

        // Zdjęcie
        public byte[]? ExistingProfilePicture { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }

        // 🚗 KLUCZOWE
        public bool IsDriver { get; set; }

        // Oceny
        // Ocena tego użytkownika jako pasażera (z PassengerProfile.Rating)
        public double PassengerRating { get; set; }

        // Ocena tego użytkownika jako kierowcy (jeśli istnieje DriverProfile). Nullable, bo nie każdy jest kierowcą.
        public double? DriverRating { get; set; }
    }
}
