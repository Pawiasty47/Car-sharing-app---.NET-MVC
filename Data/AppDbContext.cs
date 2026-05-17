using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using Microsoft.AspNetCore.Identity;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DriverProfile> DriverProfiles { get; set; }
    public DbSet<PassengerProfile> PassengerProfiles { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<LocationPoint> LocationPoints { get; set; }
    public DbSet<OfferedRide> OfferedRides { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Waypoint> Waypoints { get; set; }
    public DbSet<DriverApplication> DriverApplications { get; set; }
    public DbSet<AppReport> Reports { get; set; }
    public DbSet<Opinion> Opinions { get; set; }

    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }

    public DbSet<RideSubscription> RideSubscriptions { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        model.Entity<User>()
            .HasOne(u => u.DriverProfile)
            .WithOne(dp => dp.User)
            .HasForeignKey<DriverProfile>(dp => dp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<User>()
            .HasOne(u => u.PassengerProfile)
            .WithOne(pp => pp.User)
            .HasForeignKey<PassengerProfile>(pp => pp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Review>()
            .HasOne(r => r.FromUser)
            .WithMany(u => u.ReviewsGiven)
            .HasForeignKey(r => r.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Review>()
            .HasOne(r => r.ToUser)
            .WithMany(u => u.ReviewsReceived)
            .HasForeignKey(r => r.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Vehicle>()
            .HasOne(v => v.Owner)
            .WithMany()
            .HasForeignKey(v => v.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<OfferedRide>()
            .HasOne(o => o.Driver)
            .WithMany()
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<OfferedRide>()
            .HasOne(o => o.Vehicle)
            .WithMany()
            .HasForeignKey(o => o.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<OfferedRide>()
            .HasOne(o => o.StartLocation)
            .WithMany()
            .HasForeignKey(o => o.StartLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<OfferedRide>()
            .HasOne(o => o.EndLocation)
            .WithMany()
            .HasForeignKey(o => o.EndLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Booking>()
            .HasOne(b => b.Ride)
            .WithMany(o => o.Bookings)
            .HasForeignKey(b => b.RideId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Booking>()
            .HasOne(b => b.Passenger)
            .WithMany()
            .HasForeignKey(b => b.PassengerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Review>()
            .HasOne(r => r.Ride)
            .WithMany()
            .HasForeignKey(r => r.RideId)
            .OnDelete(DeleteBehavior.SetNull);

        model.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Booking>()
            .HasOne(b => b.Passenger)
            .WithMany()
            .HasForeignKey(b => b.PassengerUserId)
            .HasPrincipalKey(p => p.Id);

        model.Entity<Waypoint>()
            .HasOne(w => w.Ride)
            .WithMany(r => r.Waypoints)
            .HasForeignKey(w => w.RideId);

        model.Entity<Waypoint>()
            .HasOne(w => w.Location)
            .WithMany(lp => lp.Waypoints)
            .HasForeignKey(w => w.LocationPointId);

        model.Entity<DriverApplication>()
            .HasOne(d => d.Vehicle)
            .WithMany()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<AppReport>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        model.Entity<RideSubscription>()
            .HasOne(rs => rs.User)
            .WithMany()
            .HasForeignKey(rs => rs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}