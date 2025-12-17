using System.ComponentModel.DataAnnotations;

namespace projekt_zespołowy.Models
{
    public class DriverProfile
    {
        [Key]
        public Guid UserId { get; set; }
        public User User { get; set; }
        public bool IsVerified { get; set; } = false;
        public string? DrivingLicenseImageUrl { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
        public int CompletedRidesCount { get; set; }
        public double Rating { get; set; }
    }
}
