using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public class Cart
{
    [Key] public int Id { get; set; }

    public DateTime? CheckInDate { get; set; } = DateTime.Now;

    public DateTime? CheckOutDate { get; set; } = DateTime.Now;

    public int Quantity { get; set; }

    public bool IsAddBed { get; set; } = false;

    [Precision(18, 2)] public decimal TotalPrice { get; set; }

    // FK
    public int RoomCategoryId { get; set; }

    public int RoomSettingId { get; set; }

    public int UserId { get; set; }

    // Navigation
    public RoomCategory RoomCategory { get; set; } = null!;

    public RoomSetting RoomSetting { get; set; } = null!;

    public User User { get; set; } = null!;
}