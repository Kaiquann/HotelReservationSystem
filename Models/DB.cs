using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ResetPassword> ResetPasswords { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomCategory> RoomCategories { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<RoomImage> RoomImages { get; set; }
    public DbSet<RoomSetting> RoomSettings { get; set; }
    public DbSet<RoomCategoryReview> RoomCategoryReviews { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<ResetMfa> ResetMfas { get; set; }

    // Order & Payment Part
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    // Log Table Only (Do not Delete This Line)
    public DbSet<LoginLog> LoginLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Seed Roles
        builder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin" },
            new Role { Id = 2, Name = "Admin" },
            new Role { Id = 3, Name = "User" }
        );
    }

    /**
     * @ModelName: Product
     * @TableName: Products
     *
     * Remember Table Name Must Add "s" at the end
     */
    //public DbSet<Product> Products { get; set; }
}