using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models.ViewModels
{
    public class AddVehicleViewModel
    {
        [Required(ErrorMessage = "Proszę podać markę pojazdu.")]
        [Display(Name = "Marka", Prompt = "np. Toyota")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Proszę podać model pojazdu.")]
        [Display(Name = "Model", Prompt = "np. Yaris")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Numer rejestracyjny jest wymagany.")]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "Numer rejestracyjny powinien mieć od 4 do 8 znaków.")]
        [Display(Name = "Numer Rejestracyjny", Prompt = "np. WA 12345")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Dozwolone są tylko litery i cyfry.")]
        public string RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Określ całkowitą liczbę miejsc.")]
        [Range(2, 9, ErrorMessage = "Liczba miejsc musi wynosić od 2 do 9.")]
        [Display(Name = "Liczba miejsc (razem z kierowcą)")]
        public int SeatsTotal { get; set; } = 5; // Domyślna wartość

        [Required(ErrorMessage = "Kolor pojazdu jest wymagany.")]
        [Display(Name = "Kolor", Prompt = "np. Srebrny metalik")]
        public string Color { get; set; }
    }
}
