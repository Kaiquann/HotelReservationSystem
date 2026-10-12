using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public enum OrderStatus
{
    [Display(Name = "Pending")] Pending, // Initial order, waiting for payment

    [Display(Name = "Confirmed")] Confirmed, // Payment received, order confirmed

    [Display(Name = "Canceled")] Canceled, // Order was canceled

    [Display(Name = "Failed")] Failed, // Order failed (payment failed, etc.)

    [Display(Name = "Pending Refund")] PendingRefund, // Order is pending refund

    [Display(Name = "Refunded")] Refunded // Order was refunded
}

public class Order
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string OrderNumber { get; set; } = string.Empty;

    [MaxLength(255)] public string TransactionNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Precision(10, 2)] public decimal ServiceChargeRate { get; set; } = 0;

    [Precision(10, 2)] public decimal ServiceCharge { get; set; } = 0;

    [Precision(10, 2)] public decimal SubTotalAmount { get; set; } = 0;

    [Precision(10, 2)] public decimal TotalAmount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // FK
    public int UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = [];
}