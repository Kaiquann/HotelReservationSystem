using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class Room
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string RoomCode { get; set; } = string.Empty;

    public int IsDeleted { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // FK
    public int RoomCategoryId { get; set; }

    public int RoomSettingId { get; set; }

    // Navigation property
    public RoomCategory RoomCategory { get; set; } = null!;

    public RoomSetting RoomSetting { get; set; } = null!;
}