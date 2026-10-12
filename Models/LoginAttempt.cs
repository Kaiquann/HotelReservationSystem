using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class LoginAttempt
{
    [Key] public int Id { get; set; }

    public int TryAttempt { get; set; }

    [MaxLength(255)] public string? Ip { get; set; } = string.Empty;

    public bool IsBlocked { get; set; } = false;

    public DateTime ExpiredAt { get; set; } = DateTime.Now.AddMinutes(15);

    // FK
    public int UserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}