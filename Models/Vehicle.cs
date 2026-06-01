using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projekt_zespołowy.Models
{
    public class Vehicle
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }

        [Required(ErrorMessage = "Marka pojazdu jest wymagana.")]
        [StringLength(50, ErrorMessage = "Marka może mieć maksymalnie 50 znaków.")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Model pojazdu jest wymagany.")]
        [StringLength(50, ErrorMessage = "Model może mieć maksymalnie 50 znaków.")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Numer rejestracyjny jest wymagany.")]
        [StringLength(15, ErrorMessage = "Numer rejestracyjny może mieć maksymalnie 15 znaków.")]
        public string RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Całkowita liczba miejsc jest wymagana.")]
        [Range(2, 9, ErrorMessage = "Pojazd musi mieć od 2 do 9 miejsc (łącznie z kierowcą).")]
        public int SeatsTotal { get; set; }

        [Required(ErrorMessage = "Liczba dostępnych miejsc jest wymagana.")]
        [Range(1, 8, ErrorMessage = "Liczba dostępnych miejsc dla pasażerów musi być między 1 a 8.")]
        public int SeatsAvailable { get; set; }

        [Required(ErrorMessage = "Kolor pojazdu jest wymagany.")]
        [StringLength(30, ErrorMessage = "Nazwa koloru jest za długa.")]
        public string Color { get; set; }

        // To pole jest opcjonalne (znak zapytania '?' przy string), więc zostaje bez [Required]
        public string? ImageUrl { get; set; }
    }
}