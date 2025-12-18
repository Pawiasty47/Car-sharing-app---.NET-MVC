namespace projekt_zespołowy.Models
{
    public class AdminUserListVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; } // Imię + Nazwisko
        public string PhoneNumber { get; set; }
        public bool IsDriver { get; set; }   // To będzie nasze "Czy jest kierowcą"
    }
}
