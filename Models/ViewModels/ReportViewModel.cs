using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models.ViewModels
{
    public class ReportViewModel
    {
        [Required(ErrorMessage = "Wybierz kategorię problemu.")]
        [Display(Name = "Kategoria")]
        public ReportCategory Category { get; set; }

        [Required(ErrorMessage = "Opis problemu jest wymagany.")]
        [MinLength(10, ErrorMessage = "Opis musi mieć co najmniej 10 znaków.")]
        [MaxLength(1000, ErrorMessage = "Opis jest za długi.")]
        [Display(Name = "Treść zgłoszenia")]
        public string Description { get; set; }
    }
}
