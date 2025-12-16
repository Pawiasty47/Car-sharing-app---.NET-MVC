using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models
{
    public class PassengerProfile
    {
        [Key]
        public Guid UserId { get; set; }
        public User User { get; set; }

        // Statystyki
        public double Rating { get; set; }
        public int CompletedBookingsCount { get; set; }

        // Preferncje

        public bool PrefersNonSmoking { get; set; } = true; // Papierosy
        public bool PrefersQuietRide { get; set; } = false; // Rozmowa vs Cisza

        public bool PrefersMusic { get; set; } = true;      // NOWE: Muzyka
        public bool AcceptsPets { get; set; } = false;      // NOWE: Zwierzęta
        public bool AcceptsEating { get; set; } = false;    // NOWE: Jedzenie w aucie

        // --- ZDJĘCIE ---
        public byte[]? ProfilePicture { get; set; } // NOWE: Zdjęcie trzymane jako ciąg bajtów
    }
}