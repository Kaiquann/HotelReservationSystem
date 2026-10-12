using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public class RoomCategory
{
    [Key] public int Id { get; set; }

    [MaxLength(50)] public string Type { get; set; } = string.Empty;

    [Precision(18, 2)] public decimal Price { get; set; } = 0.00m;

    [MaxLength(255)] public string Description { get; set; } = string.Empty;

    public int MaxPeople { get; set; } = 1;

    public int IsDeleted { get; set; } = 0;

    public bool IsAddBed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<RoomImage> RoomImages { get; set; } = [];

    public ICollection<Cart> Carts { get; set; } = [];

    public ICollection<Room> Rooms { get; set; } = [];

    public ICollection<RoomCategoryReview> RoomCategoryReview { get; set; } = [];
}