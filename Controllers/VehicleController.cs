using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    public class VehicleController : Controller
    {
        private static List<Vehicle> _vehicles = new List<Vehicle>(); //pojazdy zapisuje w cachu ale narazie nie wykorzystujemy tego,
                                                                      //mozna zmienic na zapis do lokalnej bazy

        public IActionResult Index()
        {
            return View(_vehicles);
        }

        public IActionResult Details(Guid id)
        {
            var v = _vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return NotFound();
            return View(v);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AddVehicleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = model.Make,
                Model = model.Model,
                RegistrationNumber = model.RegistrationNumber,
                SeatsTotal = model.SeatsTotal,
                SeatsAvailable = model.SeatsTotal,
                Color = model.Color
            };

            _vehicles.Add(vehicle);

            TempData["SuccessMessage"] = "Pojazd został dodany!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            var v = _vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return NotFound();

            var model = new AddVehicleViewModel
            {
                Make = v.Make,
                Model = v.Model,
                RegistrationNumber = v.RegistrationNumber,
                SeatsTotal = v.SeatsTotal,
                Color = v.Color
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, AddVehicleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var v = _vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return NotFound();

            v.Make = model.Make;
            v.Model = model.Model;
            v.RegistrationNumber = model.RegistrationNumber;
            v.SeatsTotal = model.SeatsTotal;
            v.Color = model.Color;

            TempData["SuccessMessage"] = "Pojazd zaktualizowany!";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(Guid id)
        {
            var v = _vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return NotFound();

            return View(v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var v = _vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return NotFound();

            _vehicles.Remove(v);
            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction("Index");
        }
    }
}
