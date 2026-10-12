using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class LoginLog
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string Email { get; set; } = string.Empty;

    [MaxLength(255)] public string? Ip { get; set; } = string.Empty;

    [MaxLength(255)] public string? City { get; set; } = string.Empty;

    [MaxLength(255)] public string? Region { get; set; } = string.Empty;

    [MaxLength(255)] public string? Country { get; set; } = string.Empty;

    [MaxLength(255)] public string? Loc { get; set; } = string.Empty;

    [MaxLength(255)] public string? Org { get; set; } = string.Empty;

    [MaxLength(255)] public string? Postal { get; set; } = string.Empty;

    [MaxLength(255)] public string? Timezone { get; set; } = string.Empty;

    public DateTime LoginTime { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // FK
    public int UserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}