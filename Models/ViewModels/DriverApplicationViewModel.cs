using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace projekt_zespołowy.Models.ViewModels
{
    public class DriverApplicationViewModel
    {
        // --- SEKCJA: DANE OSOBOWE ---

        // 1. IMIĘ
        [Display(Name = "Imię")]
        [Required(ErrorMessage = "Podaj imię")]
        [RegularExpression(@"^[a-zA-ZąćęłńóśźżĄĆĘŁŃÓŚŹŻ]+$", ErrorMessage = "Imię może zawierać tylko litery (bez spacji).")]
        public string FirstName { get; set; }

        // 2. DRUGIE IMIĘ
        [Display(Name = "Drugie imię (opcjonalnie)")]
        [RegularExpression(@"^[a-zA-ZąćęłńóśźżĄĆĘŁŃÓŚŹŻ]+$", ErrorMessage = "Drugie imię może zawierać tylko litery.")]
        public string? SecondName { get; set; }

        // 3. NAZWISKO
        [Display(Name = "Nazwisko")]
        [Required(ErrorMessage = "Podaj nazwisko")]
        [RegularExpression(@"^[a-zA-ZąćęłńóśźżĄĆĘŁŃÓŚŹŻ\-]+$", ErrorMessage = "Nazwisko może zawierać tylko litery i myślnik.")]
        public string LastName { get; set; }

        // 4. NUMER DOKUMENTU (To tego brakowało!)
        [Display(Name = "Numer Dowodu Osobistego / Paszportu")]
        [Required(ErrorMessage = "Podaj numer dokumentu tożsamości")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Numer dokumentu jest za krótki lub za długi")]
        public string IdDocumentNumber { get; set; }

        // --- SEKCJA: UPRAWNIENIA ---

        [Display(Name = "Numer prawa jazdy")]
        [Required(ErrorMessage = "Podaj numer prawa jazdy")]
        public string DriverLicenseNumber { get; set; }

        [Display(Name = "Kategorie (np. B, C)")]
        [Required(ErrorMessage = "Wpisz posiadane kategorie")]
        public string LicenseCategories { get; set; }

        [Display(Name = "Data urodzenia")]
        [DataType(DataType.Date)]
        [Required]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "PESEL")]
        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z 11 cyfr")]
        public string PESEL { get; set; }

        [Display(Name = "Wybierz samochód do weryfikacji")]
        [Required(ErrorMessage = "Musisz wybrać samochód")]
        public Guid SelectedVehicleId { get; set; }

        public List<SelectListItem>? MyVehicles { get; set; }
    }
}