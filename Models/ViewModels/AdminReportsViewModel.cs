using projekt_zespołowy.Models;
using System.Collections.Generic;

namespace projekt_zespołowy.Models.ViewModels
{
    public class AdminReportsViewModel
    {
        public List<AppReport> Reports { get; set; } = new();
        public List<DriverApplication> DriverApplications { get; set; } = new();
    }
}
