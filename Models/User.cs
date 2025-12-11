using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace projekt_zespołowy.Models
{
    public class User : IdentityUser<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual DriverProfile DriverProfile { get; set; }
        public virtual PassengerProfile PassengerProfile { get; set; }
        public virtual ICollection<Review> ReviewsGiven { get; set; }
        public virtual ICollection<Review> ReviewsReceived { get; set; }
    }
}
