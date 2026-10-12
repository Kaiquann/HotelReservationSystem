using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public class RoomSetting
{
    [Key] public int Id { get; set; }

    public bool IsSmokingArea { get; set; } = false;

    public bool IsLandscapeArea { get; set; } = false;

    public bool IsCornerRoom { get; set; } = false;

    // Navigation
    public ICollection<Room> Rooms { get; set; } = [];

    public ICollection<Cart> Carts { get; set; } = [];
}