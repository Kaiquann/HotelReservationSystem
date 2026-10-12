using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class ResetPassword
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; } = DateTime.Now.AddMinutes(15);

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // FK
    public int UserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}