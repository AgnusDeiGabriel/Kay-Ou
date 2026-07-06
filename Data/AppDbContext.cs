using Microsoft.EntityFrameworkCore;
using KayAyiti.Models;

namespace KayAyiti.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<House> Houses => Set<House>();
    public DbSet<HouseImage> HouseImages => Set<HouseImage>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<HouseLike> HouseLikes => Set<HouseLike>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // House -> User (cascade delete)
        modelBuilder.Entity<House>()
            .HasOne(h => h.User)
            .WithMany(u => u.Houses)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // HouseImage -> House
        modelBuilder.Entity<HouseImage>()
            .HasOne(i => i.House)
            .WithMany(h => h.Images)
            .HasForeignKey(i => i.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Payment -> User (no cascade to avoid multiple paths)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Payment -> House
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.House)
            .WithMany(h => h.Payments)
            .HasForeignKey(p => p.HouseId)
            .OnDelete(DeleteBehavior.NoAction);

        // RefreshToken -> User
        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message -> Sender (no cascade to avoid multiple paths)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        // Message -> Receiver
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        // Message -> House
        modelBuilder.Entity<Message>()
            .HasOne(m => m.House)
            .WithMany()
            .HasForeignKey(m => m.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // HouseLike -> User (no cascade to avoid multiple paths)
        modelBuilder.Entity<HouseLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // HouseLike -> House (cascade when house deleted)
        modelBuilder.Entity<HouseLike>()
            .HasOne(l => l.House)
            .WithMany(h => h.Likes)
            .HasForeignKey(l => l.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // One like per user per house
        modelBuilder.Entity<HouseLike>()
            .HasIndex(l => new { l.UserId, l.HouseId })
            .IsUnique();

        // Report -> Reporter (no cascade to avoid multiple paths)
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Report -> House (cascade when house deleted)
        modelBuilder.Entity<Report>()
            .HasOne(r => r.House)
            .WithMany()
            .HasForeignKey(r => r.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.HouseId, r.Status });

        // Defo SQL obligatwa pou kolòn ki ajoute sou tab ki deja gen done
        modelBuilder.Entity<User>().Property(u => u.KycStatus).HasDefaultValue("None");
        modelBuilder.Entity<House>().Property(h => h.OwnershipStatus).HasDefaultValue("None");

        // Seed Admin user (password: Admin@123)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Nom = "Administrateur",
            Email = "admin@kayayiti.com",
            PasswordHash = "$2a$11$elBbDc4iVyVCok.WvfQCau2nq2ndD0kn0M67kgI962A0ojiGsTyki",
            Role = "Admin",
            Telefon = "+50999999999",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}