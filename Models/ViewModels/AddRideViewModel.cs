using projekt_zespołowy.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace projekt_zespołowy.Models.ViewModels
{
    public class LocationInputModel
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Pole 'Nazwa miejsca' jest wymagane.")]
        [Display(Name = "Nazwa/Opis miejsca")]
        public string Name { get; set; }

        // W rzeczywistej aplikacji te pola byłyby pobierane z mapy
        [Required(ErrorMessage = "Pole 'Szerokość geograficzna' jest wymagane.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Pole 'Długość geograficzna' jest wymagane.")]
        public double Longtitude { get; set; }

        [Required(ErrorMessage = "Pole 'Adres' jest wymagane.")]
        [Display(Name = "Adres")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Pole 'Miasto' jest wymagane.")]
        [Display(Name = "Miasto")]
        public string City { get; set; }
    }

    public class AddRideViewModel
    {
        // Dane przejazdu
        [Required]
        public Guid SelectedVehicleId { get; set; }

        // Miejsce startowe
        [Display(Name = "Miejsce startowe")]
        public LocationInputModel StartLocation { get; set; } = new LocationInputModel();

        // Miejsce docelowe
        [Display(Name = "Miejsce docelowe")]
        public LocationInputModel EndLocation { get; set; } = new LocationInputModel();

        // Punkty pośrednie byłyby złożoną listą, na razie uprośćmy dla widoku
        // public List<LocationInputModel> Waypoints { get; set; } = new List<LocationInputModel>();

        [Required(ErrorMessage = "Pole 'Czas odjazdu' jest wymagane.")]
        [Display(Name = "Czas odjazdu")]
        public DateTime DepartureTime { get; set; }

        [Display(Name = "Przewidywany czas przyjazdu (opcjonalnie)")]
        public DateTime? ArrivalTime { get; set; }

        [Required(ErrorMessage = "Pole 'Liczba oferowanych miejsc' jest wymagane.")]
        [Display(Name = "Liczba oferowanych miejsc")]
        [Range(1, 8, ErrorMessage = "Miejsca muszą być w zakresie od {1} do {2}.")]
        public int SeatsOffered { get; set; }

        [Required(ErrorMessage = "Pole 'Cena za miejsce' jest wymagane.")]
        [Display(Name = "Cena za miejsce")]
        [DataType(DataType.Currency)]
        [Range(0.01, 1000.00, ErrorMessage = "Cena musi być większa niż 0.")]
        public decimal PricePerSeat { get; set; }

        [Display(Name = "Elastyczna cena (możliwość negocjacji)")]
        public bool IsFlexiblePrice { get; set; } = false;

        [Display(Name = "Dodatkowe uwagi dla pasażerów")]
        [MaxLength(500)]
        public string Notes { get; set; }

        // Dane do widoku (DropDownList)
        public IEnumerable<Vehicle> AvailableVehicles { get; set; }
    }
}