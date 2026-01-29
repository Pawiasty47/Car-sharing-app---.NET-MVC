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

        public bool IsSmoker { get; set; }
        public bool PrefersQuietRide { get; set; }

        public bool PrefersMusic { get; set; }
        public bool AcceptsPets { get; set; }
        public bool AcceptsEating { get; set; }

        // --- ZDJĘCIE ---
        public byte[]? ProfilePicture { get; set; } // Zdjęcie trzymane jako ciąg bajtów
    }
}