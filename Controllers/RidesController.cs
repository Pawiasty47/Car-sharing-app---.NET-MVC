using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

public class RidesController : Controller
{
    private static List<Vehicle> _vehicles = new List<Vehicle>
    {
        new Vehicle { Id = Guid.NewGuid(), Make = "Skoda", Model = "Octavia", Color = "Niebieski" },
        new Vehicle { Id = Guid.NewGuid(), Make = "Audi", Model = "A4", Color = "Czarny" }
    };

    //crud dla przejazdow, trzeba przerobic zeby zapisywalo dane do lokalnej bazy danych, w przyszlosci do bazy w chmurze

    private static List<OfferedRide> _rides = new List<OfferedRide>();

    public IActionResult Index()
    {
        return View(_rides);
    }

    public IActionResult Details(Guid id)
    {
        var ride = _rides.FirstOrDefault(r => r.Id == id);
        if (ride == null) return NotFound();
        return View(ride);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new AddRideViewModel
        {
            AvailableVehicles = _vehicles,
            DepartureTime = DateTime.Now.AddHours(1)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AddRideViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableVehicles = _vehicles;
            return View(model);
        }

        TempData["SuccessMessage"] = "Pomyślnie dodano nowy przejazd!";
        return RedirectToAction("Index", "Rides");
    }

    public IActionResult Edit(Guid id)
    {
        var ride = _rides.FirstOrDefault(r => r.Id == id);
        if (ride == null) return NotFound();

        var model = new AddRideViewModel
        {
            SelectedVehicleId = ride.VehicleId,
            StartLocation = new LocationInputModel
            {
                Name = ride.StartLocation.Name,
                Address = ride.StartLocation.Address,
                City = ride.StartLocation.City,
                Latitude = ride.StartLocation.Latitude,
                Longtitude = ride.StartLocation.Longtitude
            },
            EndLocation = new LocationInputModel
            {
                Name = ride.EndLocation.Name,
                Address = ride.EndLocation.Address,
                City = ride.EndLocation.City,
                Latitude = ride.EndLocation.Latitude,
                Longtitude = ride.EndLocation.Longtitude
            },
            DepartureTime = ride.DepartureTime,
            ArrivalTime = ride.ArrivalTime,
            SeatsOffered = ride.SeatsOffered,
            PricePerSeat = ride.PricePerSeat,
            IsFlexiblePrice = ride.IsFlexiblePrice,
            Notes = ride.Notes,
            AvailableVehicles = _vehicles
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, AddRideViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableVehicles = _vehicles;
            return View(model);
        }

        var ride = _rides.FirstOrDefault(r => r.Id == id);
        if (ride == null) return NotFound();

        ride.VehicleId = model.SelectedVehicleId;
        ride.DepartureTime = model.DepartureTime;
        ride.ArrivalTime = model.ArrivalTime;
        ride.SeatsOffered = model.SeatsOffered;
        ride.PricePerSeat = model.PricePerSeat;
        ride.IsFlexiblePrice = model.IsFlexiblePrice;
        ride.Notes = model.Notes;

        TempData["SuccessMessage"] = "Przejazd zaktualizowany!";
        return RedirectToAction("Index");
    }

    public IActionResult Delete(Guid id)
    {
        var ride = _rides.FirstOrDefault(r => r.Id == id);
        if (ride == null) return NotFound();
        return View(ride);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        var ride = _rides.FirstOrDefault(r => r.Id == id);
        if (ride == null) return NotFound();

        _rides.Remove(ride);
        TempData["SuccessMessage"] = "Przejazd usunięty!";
        return RedirectToAction("Index");
    }
}
