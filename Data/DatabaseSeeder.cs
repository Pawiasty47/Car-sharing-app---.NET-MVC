using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var random = new Random();


            // 1. Użytkownicy

            if (!await db.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    new User { Id = Guid.NewGuid(), FirstName = "Jan", LastName = "Nowak", Email = "jan.nowak@test.pl", PhoneNumber = "500100200" },
                    new User { Id = Guid.NewGuid(), FirstName = "Anna", LastName = "Kowalska", Email = "anna.kowalska@test.pl", PhoneNumber = "500100201" },
                    new User { Id = Guid.NewGuid(), FirstName = "Piotr", LastName = "Wiśniewski", Email = "piotr.wisniewski@test.pl", PhoneNumber = "500100202" },
                    new User { Id = Guid.NewGuid(), FirstName = "Katarzyna", LastName = "Lewandowska", Email = "katarzyna.lew@test.pl", PhoneNumber = "500100203" },
                    new User { Id = Guid.NewGuid(), FirstName = "Marek", LastName = "Zieliński", Email = "marek.zielinski@test.pl", PhoneNumber = "500100204" }
                };

                db.Users.AddRange(users);
                await db.SaveChangesAsync();
            }

            var allUsers = await db.Users.ToListAsync();


            // 2. DriverProfiles

            foreach (var user in allUsers)
            {
                if (!await db.DriverProfiles.AnyAsync(dp => dp.UserId == user.Id))
                {
                    db.DriverProfiles.Add(new DriverProfile
                    {
                        UserId = user.Id,
                        DrivingLicenseImageUrl = "placeholder.jpg",
                        CompletedRidesCount = 0,
                        Rating = 0
                    });
                }
            }
            await db.SaveChangesAsync();

            var allDriverProfiles = await db.DriverProfiles.ToListAsync();

            // 3. Vehicles

            if (!await db.Vehicles.AnyAsync())
            {
                var sampleVehicles = new List<Vehicle>
                {
                    new Vehicle { OwnerId = allDriverProfiles[0].UserId, Make = "Toyota", Model = "Corolla", RegistrationNumber = "KR1234A", SeatsTotal = 5, SeatsAvailable = 4, Color = "Czarny" },
                    new Vehicle { OwnerId = allDriverProfiles[1].UserId, Make = "Honda", Model = "Civic", RegistrationNumber = "WA5678B", SeatsTotal = 5, SeatsAvailable = 3, Color = "Biały" },
                    new Vehicle { OwnerId = allDriverProfiles[2].UserId, Make = "Ford", Model = "Focus", RegistrationNumber = "GD2222C", SeatsTotal = 5, SeatsAvailable = 4, Color = "Niebieski" },
                    new Vehicle { OwnerId = allDriverProfiles[3].UserId, Make = "BMW", Model = "320d", RegistrationNumber = "PO9999D", SeatsTotal = 5, SeatsAvailable = 2, Color = "Czerwony" },
                    new Vehicle { OwnerId = allDriverProfiles[4].UserId, Make = "Audi", Model = "A4", RegistrationNumber = "SZ1111E", SeatsTotal = 5, SeatsAvailable = 4, Color = "Srebrny" }
                };

                db.Vehicles.AddRange(sampleVehicles);
                await db.SaveChangesAsync();
            }

            var allVehicles = await db.Vehicles.ToListAsync();


            // 4. LocationPoints 

            var locations = await db.LocationPoints.ToListAsync();
            if (!locations.Any())
            {
                string[] miastaStart = { "Kraków", "Warszawa", "Gdańsk", "Wrocław", "Poznań" };
                string[] miastaEnd = { "Zakopane", "Łódź", "Sopot", "Opole", "Szczecin" };

                locations = new List<LocationPoint>();
                for (int i = 0; i < 5; i++)
                {
                    locations.Add(new LocationPoint
                    {
                        Name = miastaStart[i],
                        Latitude = 50 + random.NextDouble(),
                        Longtitude = 19 + random.NextDouble(),
                        Address = $"{miastaStart[i]} - centrum",
                        City = miastaStart[i]
                    });
                    locations.Add(new LocationPoint
                    {
                        Name = miastaEnd[i],
                        Latitude = 52 + random.NextDouble(),
                        Longtitude = 20 + random.NextDouble(),
                        Address = $"{miastaEnd[i]} - centrum",
                        City = miastaEnd[i]
                    });
                }

                db.LocationPoints.AddRange(locations);
                await db.SaveChangesAsync();
            }

            locations = await db.LocationPoints.ToListAsync();

            // 5. OfferedRides

            if (!await db.OfferedRides.AnyAsync())
            {
                var rides = new List<OfferedRide>();
                for (int i = 0; i < 5; i++)
                {
                    rides.Add(new OfferedRide
                    {
                        DriverId = allDriverProfiles[i % allDriverProfiles.Count].UserId,
                        VehicleId = allVehicles[i % allVehicles.Count].Id,
                        StartLocationId = locations[i * 2].Id,
                        EndLocationId = locations[i * 2 + 1].Id,
                        DepartureTime = DateTime.Now.AddHours(i + 1),
                        ArrivalTime = DateTime.Now.AddHours(i + 3),
                        SeatsOffered = allVehicles[i % allVehicles.Count].SeatsTotal - 1,
                        SeatsTaken = 0,
                        PricePerSeat = 15 + i * 5,
                        Notes = $"Przejazd z {locations[i * 2].Name} do {locations[i * 2 + 1].Name}",
                        Status = RideStatus.Published,
                        Waypoints = new List<Waypoint>()
                    });
                }

                db.OfferedRides.AddRange(rides);
                await db.SaveChangesAsync();
            }
        }
    }
}
