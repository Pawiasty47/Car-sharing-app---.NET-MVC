# Car Sharing App

A ride-sharing web application built with **ASP.NET Core 8 MVC**. It connects drivers offering spare seats with passengers looking for trips between cities.

## Features

### For passengers
- Account registration and login
- Search and browse available rides
- Book seats on rides
- Passenger profile with preferences (e.g. quiet ride, smoking)
- Booking and payment history
- Ratings and reviews after completed trips
- Chat with the driver and other ride participants
- Notifications about booking and ride status
- Ride subscriptions (alerts for specific routes)

### For drivers
- Driver profile with details and rating
- Vehicle management (create, edit, delete)
- Create and publish ride offers with waypoints
- Accept or reject passenger bookings
- Driver application flow (admin verification)

### Admin panel
- Dashboard with user and ride statistics
- User and role management
- Report and review moderation
- Driver application verification

## Tech stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 8, MVC |
| Database | SQL Server (LocalDB) |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity |
| Frontend | Razor Views, Bootstrap, jQuery |

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (installed with Visual Studio) or full SQL Server
- Visual Studio 2022 / VS Code / Rider (optional)

## Local setup

### 1. Clone the repository

```bash
git clone https://github.com/YOUR-USERNAME/car-sharing-app.git
cd car-sharing-app
```

### 2. Configure the database connection

The default connection string in `appsettings.json` points to LocalDB:

```
Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=RideSharingDb;Integrated Security=True;...
```

If you use a different SQL Server instance, update `ConnectionStrings:DefaultConnection` in `appsettings.json` or `appsettings.Development.json`.

### 3. Apply database migrations

```bash
dotnet ef database update
```

> Migrations are included in the repository. On first run, the app automatically seeds the database with test data (`DatabaseSeeder`).

### 4. Run the application

```bash
dotnet run
```

The app starts at `https://localhost:7xxx` by default (port defined in `Properties/launchSettings.json`).

## Test accounts

After the first run, the seeder creates these accounts:

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@projekt.pl` | `admin11` |
| Passenger / Driver | `jan.nowak@test.pl` | `Test123!` |
| Passenger / Driver | `anna.kowalska@test.pl` | `Test123!` |
| Passenger / Driver | `piotr.wisniewski@test.pl` | `Test123!` |

The seeder also adds sample vehicles, locations, and published rides between Polish cities.

## Project structure

```
├── Controllers/       # MVC controllers (Rides, Booking, Admin, Chat...)
├── Models/            # Domain models and view models
├── Views/             # Razor views
├── Data/              # DbContext and database seeder
├── Services/          # Application services (e.g. notifications)
├── Migrations/        # Entity Framework migrations
└── wwwroot/           # Static files (CSS, JS, images)
```

## User roles

- **Admin** — full access to the admin panel
- **Passenger** — search rides, make bookings, leave reviews
- **Driver** — manage vehicles and ride offers

A single user can hold both passenger and driver roles.

## License

Team project — use according to the repository terms.
