using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Models;

namespace MisterTicket.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationSeat> ReservationSeats { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configuration Event
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Court)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CourtId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration Court -> Seat
            modelBuilder.Entity<Court>()
                .HasMany(c => c.Seats)
                .WithOne(s => s.Court)
                .HasForeignKey(s => s.CourtId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration Seat
            modelBuilder.Entity<Seat>()
                .HasIndex(s => new { s.CourtId, s.SeatNumber })
                .IsUnique();

            // ✅ Configuration ReservationSeat - CLÉ COMPOSITE
            modelBuilder.Entity<ReservationSeat>()
                .HasKey(rs => new { rs.ReservationId, rs.SeatId });

            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Reservation)
                .WithMany(r => r.ReservationSeats)
                .HasForeignKey(rs => rs.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Seat)
                .WithMany(s => s.ReservationSeats)
                .HasForeignKey(rs => rs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Reservations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration Payment -> Reservation (One-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Reservation)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration des colonnes decimal avec précision
            modelBuilder.Entity<Seat>()
                .Property(s => s.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // Configuration des statuts par défaut
            modelBuilder.Entity<Seat>()
                .Property(s => s.Status)
                .HasDefaultValue("Available");

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<Reservation>()
                .Property(r => r.ReservationDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentDate)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasDefaultValue("Pending");
        }
    }
}