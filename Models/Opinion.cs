using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models
{
    public class Opinion
    {
        [Key]
        public int Id { get; set; }

        // Kto dodał opinię
        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Proszę wpisać treść opinii.")]
        [StringLength(500, ErrorMessage = "Opinia może mieć maksymalnie 500 znaków.")]
        public string Content { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // Ilość gwiazdek (1-5)

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}