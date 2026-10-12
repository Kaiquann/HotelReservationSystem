using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public enum BookingStatus
{
    [Display(Name = "Pending")] Pending, // Initial booking, waiting for payment

    [Display(Name = "Confirmed")] Confirmed, // Payment received, booking confirmed

    [Display(Name = "Checked In")] CheckedIn, // Guest has arrived and checked in

    [Display(Name = "Checked Out")] CheckedOut, // Guest has completed their stay

    [Display(Name = "Canceled")] Canceled, // Booking was canceled

    [Display(Name = "Failed")] Failed, // Booking failed (payment failed, etc.)

    [Display(Name = "Pending Refund")] PendingRefund, // Booking is pending refund

    [Display(Name = "Refunded")] Refunded // Booking was refunded
}

public class Booking
{
    public int Id { get; set; }

    [MaxLength(10)] public string RoomNumber { get; set; } = string.Empty;

    public bool IsAddBed { get; set; } = false;

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    [Precision(18, 2)] public decimal TotalAmount { get; set; }

    // When user checks in, this field will be updated
    public DateTime? WhenCheckInDateTime { get; set; }

    // When user checks out, this field will be updated
    public DateTime? WhenCheckOutDateTime { get; set; }

    // FK
    [ForeignKey("User")] public int UserId { get; set; }

    [ForeignKey("Room")] public int RoomId { get; set; }

    [ForeignKey("Order")] public int OrderId { get; set; }

    // Navigation property
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Room Room { get; set; } = null!;

    public Order Order { get; set; } = null!;
}