using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

public enum RoomCategoryReviewStatus
{
    [Display(Name = "Pending Review")] PendingReview,
    Approved,
    Rejected
}

public class RoomCategoryReview
{
    [Key] public int Id { get; set; }

    [MaxLength(10000)] public string Message { get; set; } = string.Empty;

    public RoomCategoryReviewStatus Status { get; set; } = RoomCategoryReviewStatus.PendingReview;

    public int Rating { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // FK
    public int UserId { get; set; }
    public int RoomCategoryId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public RoomCategory RoomCategory { get; set; } = null!;
}