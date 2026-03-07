namespace projekt_zespołowy.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public int UsersCount { get; set; }
        public int RidesCount { get; set; }

        public bool IsLoggedIn { get; set; }
        public bool IsPassenger { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDriver { get; set; }

        public List<Notification> UnreadNotifications { get; set; } = new List<Notification>();
        public List<projekt_zespołowy.Models.Opinion>? RecentOpinions { get; set; }
        public int TotalOpinions { get; set; }
    }
}