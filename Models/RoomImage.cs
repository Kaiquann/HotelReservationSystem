using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class RoomImage
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string ImagePath { get; set; } = string.Empty;

    public byte[]? ImageData { get; set; }

    [MaxLength(100)] public string? ContentType { get; set; }

    public int RoomCategoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public RoomCategory RoomCategory { get; set; } = null!;
}
